using System;
using System.IO;
using System.Threading.Tasks;
using Android.Graphics;
using Android.Util;
using Xamarin.Forms;
using AChatFull.Utils;
using Color = Android.Graphics.Color;
using AUri = Android.Net.Uri;


[assembly: Dependency(typeof(AChatFull.Droid.Services.AvatarIconBuilder_Android))]
namespace AChatFull.Droid.Services
{
    /// <summary>
    /// Рисует круглую иконку с локальной аватаркой (файл/content-uri/ресурс) и точкой статуса.
    /// Без сетевых вызовов.
    /// Совместимо с C# 7.3.
    /// </summary>
    public sealed class AvatarIconBuilder_Android : IAvatarIconBuilder
    {
        public async Task<ImageSource> BuildAsync(string avatarUrlOrPath, string initials, string presence, int sizeDp, int? statusSizeDp = null)
        {
            var ctx = Android.App.Application.Context;
            var dm = ctx.Resources.DisplayMetrics;

            int sizePx = (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, sizeDp, dm);
            int statusPx = (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, statusSizeDp ?? Math.Max(6, sizeDp / 3), dm);
            int borderPx = Math.Max(1, sizePx / 24);

            using (var bmp = Bitmap.CreateBitmap(sizePx, sizePx, Bitmap.Config.Argb8888))
            using (var canvas = new Canvas(bmp))
            {
                using (var paint = new Paint(PaintFlags.AntiAlias))
                {
                    paint.Color = Color.Transparent;
                    canvas.DrawColor(Color.Transparent);

                    // Фон круга
                    paint.Color = Color.Rgb(230, 233, 238);
                    float cx = sizePx / 2f, cy = sizePx / 2f, r = (sizePx - borderPx) / 2f;
                    canvas.DrawCircle(cx, cy, r, paint);

                    // ЛОКАЛЬНАЯ загрузка аватарки
                    Bitmap avatar = null;
                    try
                    {
                        avatar = await Task.Run<Bitmap>(() => TryLoadLocalBitmap(avatarUrlOrPath)).ConfigureAwait(false);
                    }
                    catch
                    {
                        avatar = null;
                    }

                    if (avatar != null)
                    {
                        using (var shader = new BitmapShader(avatar, Shader.TileMode.Clamp, Shader.TileMode.Clamp))
                        using (var matrix = new Matrix())
                        {
                            float scale = Math.Max(sizePx / (float)avatar.Width, sizePx / (float)avatar.Height);
                            matrix.SetScale(scale, scale);
                            float dx = (sizePx - avatar.Width * scale) * 0.5f;
                            float dy = (sizePx - avatar.Height * scale) * 0.5f;
                            matrix.PostTranslate(dx, dy);
                            shader.SetLocalMatrix(matrix);

                            paint.SetShader(shader);
                            canvas.DrawCircle(cx, cy, r, paint);
                            paint.SetShader(null);
                        }
                        avatar.Dispose();
                    }
                    else
                    {
                        // Инициалы
                        paint.Color = Color.Rgb(96, 104, 116);
                        paint.TextAlign = Paint.Align.Center;
                        paint.SetStyle(Paint.Style.Fill);
                        paint.TextSize = sizePx * 0.48f; // чуть крупнее, чтобы лучше читалось
                        var text = string.IsNullOrWhiteSpace(initials) ? "?" : initials.Trim().ToUpperInvariant();
                        var fm = paint.GetFontMetrics();
                        float baseline = cy - (fm.Ascent + fm.Descent) / 2;
                        canvas.DrawText(text, cx, baseline, paint);
                    }

                    // Белый бордер
                    using (var borderPaint = new Paint(PaintFlags.AntiAlias))
                    {
                        borderPaint.Color = Color.White;
                        borderPaint.SetStyle(Paint.Style.Stroke);
                        borderPaint.StrokeWidth = borderPx;
                        canvas.DrawCircle(cx, cy, r - borderPx / 2f, borderPaint);
                    }

                    // Индикатор статуса (правый нижний угол)
                    int dot = statusPx;
                    float dotCx = sizePx - dot * 0.55f;
                    float dotCy = sizePx - dot * 0.55f;

                    using (var dotBg = new Paint(PaintFlags.AntiAlias))
                    {
                        dotBg.Color = Color.White;
                        canvas.DrawCircle(dotCx, dotCy, dot * 0.60f, dotBg);
                    }
                    using (var dotPaint = new Paint(PaintFlags.AntiAlias))
                    {
                        dotPaint.Color = MapPresenceToAndroidColor(presence);
                        canvas.DrawCircle(dotCx, dotCy, dot * 0.50f, dotPaint);
                    }
                }

                using (var ms = new MemoryStream())
                {
                    bmp.Compress(Bitmap.CompressFormat.Png, 100, ms);
                    var bytes = ms.ToArray();
                    return ImageSource.FromStream(() => new MemoryStream(bytes, 0, bytes.Length, false, true));
                }
            }
        }

        private static Color MapPresenceToAndroidColor(string presence)
        {
            var p = (presence ?? string.Empty).Trim().ToLowerInvariant();
            switch (p)
            {
                case "online": return Color.Rgb(52, 199, 89);
                case "away": return Color.Rgb(255, 204, 0);
                case "busy": return Color.Rgb(255, 59, 48);
                default: return Color.Rgb(174, 174, 178);
            }
        }

        private static Bitmap TryLoadLocalBitmap(string pathOrResource)
        {
            if (string.IsNullOrWhiteSpace(pathOrResource))
                return null;

            var ctx = Android.App.Application.Context;

            try
            {
                // content:// URI
                if (pathOrResource.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
                {
                    var uri = AUri.Parse(pathOrResource);
                    using (var stream = ctx.ContentResolver.OpenInputStream(uri))
                    {
                        return BitmapFactory.DecodeStream(stream);
                    }
                }

                // file:// URI
                if (pathOrResource.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                {
                    var uri = AUri.Parse(pathOrResource);
                    string fsPath = uri.Path;
                    return BitmapFactory.DecodeFile(fsPath);
                }

                // Абсолютный путь или просто существующий файл
                if (System.IO.Path.IsPathRooted(pathOrResource) || System.IO.File.Exists(pathOrResource))
                {
                    return BitmapFactory.DecodeFile(pathOrResource);
                }

                // Имя ресурса в drawable (например, "my_avatar" или "my_avatar.png")
                string name = System.IO.Path.GetFileNameWithoutExtension(pathOrResource);
                if (!string.IsNullOrEmpty(name))
                {
                    int resId = ctx.Resources.GetIdentifier(name.ToLowerInvariant(), "drawable", ctx.PackageName);
                    if (resId != 0)
                        return BitmapFactory.DecodeResource(ctx.Resources, resId);
                }

                // Попытка как asset
                try
                {
                    using (var s = ctx.Assets.Open(pathOrResource))
                        return BitmapFactory.DecodeStream(s);
                }
                catch { }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}