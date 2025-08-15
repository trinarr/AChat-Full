using Android.Content;
using Android.Views;
using Android.Widget;
using Google.Android.Material.BottomNavigation;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Xamarin.Forms.Platform.Android.AppCompat;

[assembly: ExportRenderer(typeof(TabbedPage), typeof(AChatFull.Droid.Renderers.MainTabsPageRenderer))]
namespace AChatFull.Droid.Renderers
{
    /// <summary>
    /// Убирает tint у иконок TabbedPage и снижает альфу у неактивных,
    /// не вмешиваясь в обработку кликов BottomNavigationView.
    /// </summary>
    public class MainTabsPageRenderer : TabbedPageRenderer
    {
        // Непрозрачность: активная = 100%, неактивные ≈ 55%
        const int SelectedAlpha = 255;
        const int UnselectedAlpha = 180;

        public MainTabsPageRenderer(Context ctx) : base(ctx) { }

        protected override void OnElementChanged(ElementChangedEventArgs<TabbedPage> e)
        {
            base.OnElementChanged(e);
            ApplyAll(this);
        }

        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            base.OnLayout(changed, l, t, r, b);
            ApplyAll(this);
        }

        protected override void OnElementPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);
            if (e.PropertyName == nameof(TabbedPage.CurrentPage) ||
                e.PropertyName == nameof(TabbedPage.SelectedItem) ||
                e.PropertyName == nameof(Page.IconImageSource))
            {
                ApplyAll(this);
            }
        }

        void ApplyAll(Android.Views.View root)
        {
            if (root == null) return;

            if (root is BottomNavigationView bnv)
            {
                // 1) Отключаем только иконный tint (текст не трогаем)
                bnv.ItemIconTintList = null;
                // НЕ делаем: bnv.ItemTextColor = null;

                // 2) Применяем альфу к ImageView
                UpdateIconAlpha(bnv);
                return;
            }

            if (root is ViewGroup vg)
            {
                for (int i = 0; i < vg.ChildCount; i++)
                    ApplyAll(vg.GetChildAt(i));
            }
        }

        void UpdateIconAlpha(BottomNavigationView bnv)
        {
            var menu = bnv.Menu;
            if (menu == null) return;

            var selectedId = bnv.SelectedItemId;

            // Внутренний контейнер пунктов меню
            var menuView = bnv.GetChildAt(0) as ViewGroup;
            if (menuView == null) return;

            for (int i = 0; i < menuView.ChildCount; i++)
            {
                var itemView = menuView.GetChildAt(i) as ViewGroup;
                if (itemView == null) continue;

                var item = i < menu.Size() ? menu.GetItem(i) : null;
                bool isSelected = item != null && item.ItemId == selectedId;
                int alpha = isSelected ? SelectedAlpha : UnselectedAlpha;

                SetImageAlphaRecursive(itemView, alpha);
            }
        }

        // Ставим альфу только на изображения (иконки)
        void SetImageAlphaRecursive(Android.Views.View v, int alpha)
        {
            if (v is ImageView iv)
            {
                iv.ImageAlpha = alpha;
                return;
            }

            if (v is ViewGroup vg)
            {
                for (int i = 0; i < vg.ChildCount; i++)
                    SetImageAlphaRecursive(vg.GetChildAt(i), alpha);
            }
        }
    }
}