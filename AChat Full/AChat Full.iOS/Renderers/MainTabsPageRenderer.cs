using System.ComponentModel;
using CoreGraphics;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(TabbedPage), typeof(AChatFull.iOS.Renderers.MainTabsPageRenderer))]
namespace AChatFull.iOS.Renderers
{
    /// <summary>
    /// Отключает template-tint и делает неактивные иконки полупрозрачными.
    /// Подписка через Element.PropertyChanged.
    /// </summary>
    public class MainTabsPageRenderer : TabbedRenderer
    {
        // 55% непрозрачности для неактивной
        const float UnselectedAlpha = 0.55f;
        bool _subscribed;

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            if (!_subscribed && Element != null)
            {
                Element.PropertyChanged += OnTabbedPropertyChanged;
                _subscribed = true;
            }

            ApplyOriginalRenderingAndAlpha();
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();
            ApplyOriginalRenderingAndAlpha();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _subscribed && Element != null)
            {
                Element.PropertyChanged -= OnTabbedPropertyChanged;
                _subscribed = false;
            }
            base.Dispose(disposing);
        }

        void OnTabbedPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TabbedPage.CurrentPage) ||
                e.PropertyName == nameof(TabbedPage.SelectedItem) ||
                e.PropertyName == nameof(Page.IconImageSource) ||
                e.PropertyName == nameof(Page.Title))
            {
                ApplyOriginalRenderingAndAlpha();
            }
        }

        void ApplyOriginalRenderingAndAlpha()
        {
            if (TabBar == null || TabBar.Items == null) return;

            var selected = TabBar.SelectedItem;
            foreach (var item in TabBar.Items)
            {
                if (item == null) continue;

                // цветная исходная картинка
                var baseImg = item.SelectedImage ?? item.Image;
                if (baseImg == null) continue;

                var full = baseImg.ImageWithRenderingMode(UIImageRenderingMode.AlwaysOriginal);
                var dim = WithAlpha(full, UnselectedAlpha).ImageWithRenderingMode(UIImageRenderingMode.AlwaysOriginal);

                if (item == selected)
                {
                    item.Image = dim;          // запасное для неактивного
                    item.SelectedImage = full; // активная — полностью непрозрачная
                }
                else
                {
                    item.Image = dim;          // неактивная — приглушённая
                    item.SelectedImage = full; // при выборе станет непрозрачной
                }
            }

            if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
                TabBar.UnselectedItemTintColor = null;
        }

        static UIImage WithAlpha(UIImage image, float alpha)
        {
            var rect = new CGRect(0, 0, image.Size.Width, image.Size.Height);
            UIGraphics.BeginImageContextWithOptions(image.Size, false, image.CurrentScale);
            image.Draw(rect, CGBlendMode.Normal, alpha);
            var result = UIGraphics.GetImageFromCurrentImageContext();
            UIGraphics.EndImageContext();
            return result ?? image;
        }
    }
}