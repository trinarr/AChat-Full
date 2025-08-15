using System.Collections.Generic;
using Android.Content;
using Android.Views;
using Google.Android.Material.BottomNavigation;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Xamarin.Forms.Platform.Android.AppCompat;

[assembly: ExportRenderer(typeof(TabbedPage), typeof(AChatFull.Droid.Renderers.MainTabsPageRenderer))]
namespace AChatFull.Droid.Renderers
{
    /// <summary>
    /// Отключает tint у иконок TabbedPage и снижает альфу у неактивных иконок.
    /// Работает даже когда Material BottomNavigation переиспользует drawable.
    /// </summary>
    public class MainTabsPageRenderer : TabbedPageRenderer
    {
        // Непрозрачность: 100% для выбранной, ~55% для остальных
        const int SelectedAlpha = 255;
        const int UnselectedAlpha = 180;

        // Чтобы не подписываться на один и тот же BottomNavigationView много раз
        static readonly HashSet<int> _wired = new HashSet<int>();

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

        // Находит все BottomNavigationView в иерархии, отключает tint и выставляет альфу
        void ApplyAll(Android.Views.View root)
        {
            if (root == null) return;

            if (root is BottomNavigationView bnv)
            {
                DisableTint(bnv);
                UpdateIconAlpha(bnv);

                // Разовая подписка на изменение выбранного пункта
                var key = bnv.GetHashCode();
                if (!_wired.Contains(key))
                {
                    _wired.Add(key);
                    bnv.NavigationItemSelected += (s, e) =>
                    {
                        // Дадим навигации отработать, затем обновим альфы
                        Post(() => UpdateIconAlpha(bnv));
                    };
                }
                return;
            }

            if (root is ViewGroup vg)
            {
                for (int i = 0; i < vg.ChildCount; i++)
                    ApplyAll(vg.GetChildAt(i));
            }
        }

        void DisableTint(BottomNavigationView bnv)
        {
            // Важно: так Material не будет красить иконки в «системный» цвет
            bnv.ItemIconTintList = null;
            bnv.ItemTextColor = null;
        }

        void UpdateIconAlpha(BottomNavigationView bnv)
        {
            var menu = bnv.Menu;
            if (menu == null) return;

            // Корневой контейнер иконок/лейблов в BottomNavigationView
            var menuView = bnv.GetChildAt(0) as ViewGroup;
            if (menuView == null) return;

            // Пройдёмся по пунктам и поставим альфу на их ImageView
            for (int i = 0; i < menuView.ChildCount; i++)
            {
                var itemView = menuView.GetChildAt(i) as ViewGroup;
                if (itemView == null) continue;

                bool isChecked = (i < menu.Size()) && menu.GetItem(i).IsChecked;
                int alpha = isChecked ? SelectedAlpha : UnselectedAlpha;

                SetImageAlphaRecursive(itemView, alpha);
            }
        }

        // Рекурсивно ищем все ImageView-подклассы и выставляем им альфу
        void SetImageAlphaRecursive(Android.Views.View v, int alpha)
        {
            if (v is Android.Widget.ImageView iv)
            {
                // ImageAlpha — самый стабильный способ, не зависит от сост. drawable
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