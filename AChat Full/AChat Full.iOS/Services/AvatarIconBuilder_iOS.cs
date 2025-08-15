using System;
using System.IO;
using System.Threading.Tasks;
using CoreGraphics;
using Foundation;
using UIKit;
using Xamarin.Forms;
using AChatFull.Utils;

[assembly: Dependency(typeof(AChatFull.iOS.Services.AvatarIconBuilder_iOS))]
namespace AChatFull.iOS.Services
{
    /// <summary>
    /// Рисует круглую иконку с локальной аватаркой (файл/ресурс) и точкой статуса.
    /// Без сетевых вызовов. Совместимо с C# 7.3.
    /// </summary>
    public sealed class AvatarIconBuilder_iOS : IAvatarIconBuilder
    {
        public async Task<ImageSource> BuildAsync(string avatarUrlOrPath, string initials, string presence, int sizeDp, int? statusSizeDp = null)
        {
            // На iOS dp ~= points, но рисуем в пикселях с учётом scale
            nfloat scale = UIScreen.MainScreen.Scale;
            nfloat sizePt = sizeDp;
            nfloat px = sizePt * scale;
            nfloat status = (statusSizeDp ?? Math.Max(6, sizeDp / 3)) * scale;
            nfloat border = (nfloat)Math.Max(1, px / 24);

            using (var colorSpace = CGColorSpace.CreateDeviceRGB())
            using (var ctx = new CGBitmapContext(IntPtr.Zero, (int)px, (int)px, 8, 0, colorSpace, CGImageAlphaInfo.PremultipliedLast))
            {
                var center = new CGPoint(px / 2, px / 2);
                nfloat radius = (px - border) / 2f;

                // Фон круга
                ctx.SetFillColor(UIColor.FromRGB(230, 233, 238).CGColor);
                ctx.AddArc(center.X, center.Y, radius, 0, (nfloat)(2 * Math.PI), true);
                ctx.FillPath();

                // ЛОКАЛЬНАЯ загрузка аватарки
                UIImage avatar = null;
                try
                {
                    avatar = await Task.Run<UIImage>(() => TryLoadLocalImage(avatarUrlOrPath)).ConfigureAwait(false);
                }
                catch
                {
                    avatar = null;
                }

                if (avatar != null)
                {
                    // Обрезка в круг + центр-кроп
                    ctx.SaveState();
                    ctx.AddArc(center.X, center.Y, radius, 0, (nfloat)(2 * Math.PI), true);
                    ctx.Clip();

                    var imgSize = avatar.Size;
                    var scaleImg = Math.Max(px / imgSize.Width, px / imgSize.Height);
                    var drawW = imgSize.Width * scaleImg;
                    var drawH = imgSize.Height * scaleImg;
                    var drawRect = new CGRect((px - drawW) / 2, (px - drawH) / 2, drawW, drawH);
                    ctx.DrawImage(drawRect, avatar.CGImage);

                    ctx.RestoreState();
                }
                else
                {
                    // Инициалы
                    var txt = string.IsNullOrWhiteSpace(initials) ? "?" : initials.Trim().ToUpperInvariant();
                    using (var font = UIFont.SystemFontOfSize(sizePt * 0.48f, UIFontWeight.Semibold))
                    {
                        var attr = new UIStringAttributes
                        {
                            ForegroundColor = UIColor.FromRGB(96, 104, 116),
                            Font = font,
                            ParagraphStyle = new NSMutableParagraphStyle { Alignment = UITextAlignment.Center }
                        };
                        using (var ns = new NSString(txt))
                        {
                            var bounds = ns.GetBoundingRect(new CGSize(px, px), NSStringDrawingOptions.UsesLineFragmentOrigin, attr, null);
                            var textRect = new CGRect(0, (px - bounds.Height) / 2, px, bounds.Height);
                            ns.DrawString(textRect, attr);
                        }
                    }
                }

                // Белый бордер
                ctx.SetStrokeColor(UIColor.White.CGColor);
                ctx.SetLineWidth(border);
                ctx.AddArc(center.X, center.Y, radius - border / 2f, 0, (nfloat)(2 * Math.PI), true);
                ctx.StrokePath();

                // Индикатор статуса
                var dotBgRect = new CGRect(px - status * 1.1f, px - status * 1.1f, status * 1.1f, status * 1.1f);
                ctx.SetFillColor(UIColor.White.CGColor);
                ctx.FillEllipseInRect(dotBgRect);

                var dotRect = new CGRect(px - status, px - status, status, status);
                ctx.SetFillColor(MapPresenceToUIColor(presence).CGColor);
                ctx.FillEllipseInRect(dotRect);

                using (var cg = ctx.ToImage())
                using (var ui = new UIImage(cg, scale, UIImageOrientation.Up))
                {
                    var outBytes = ui.AsPNG().ToArray();
                    return ImageSource.FromStream(() => new MemoryStream(outBytes, 0, outBytes.Length, false, true));
                }
            }
        }

        private static UIColor MapPresenceToUIColor(string presence)
        {
            var p = (presence ?? string.Empty).Trim().ToLowerInvariant();
            switch (p)
            {
                case "online": return UIColor.FromRGB(52, 199, 89);
                case "away": return UIColor.FromRGB(255, 204, 0);
                case "busy": return UIColor.FromRGB(255, 59, 48);
                default: return UIColor.FromRGB(174, 174, 178);
            }
        }

        private static UIImage TryLoadLocalImage(string pathOrResource)
        {
            if (string.IsNullOrWhiteSpace(pathOrResource))
                return null;

            try
            {
                // file:// URI
                if (pathOrResource.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                {
                    var nsurl = new NSUrl(pathOrResource);
                    return UIImage.FromFile(nsurl.Path);
                }

                // Абсолютный путь
                if (pathOrResource.StartsWith("/", StringComparison.Ordinal))
                {
                    return UIImage.FromFile(pathOrResource);
                }

                // Ресурс в бандле (имя с/без расширения)
                var name = System.IO.Path.GetFileNameWithoutExtension(pathOrResource);
                var ext = System.IO.Path.GetExtension(pathOrResource);
                string extNoDot = string.IsNullOrEmpty(ext) ? null : ext.TrimStart('.');

                string bundlePath = null;
                if (string.IsNullOrEmpty(extNoDot))
                {
                    bundlePath = NSBundle.MainBundle.PathForResource(name, null);
                }
                else
                {
                    bundlePath = NSBundle.MainBundle.PathForResource(name, extNoDot);
                }

                if (!string.IsNullOrEmpty(bundlePath))
                    return UIImage.FromFile(bundlePath);

                // Попытка через FromBundle (сам подберёт @2x/@3x)
                var img = UIImage.FromBundle(pathOrResource);
                return img;
            }
            catch
            {
                return null;
            }
        }
    }
}