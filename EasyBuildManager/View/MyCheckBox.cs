using System.Windows.Controls;

namespace EasyBuildManager.View
{
    class MyCheckBox : CheckBox
    {
        protected override void OnToggle()
        {
            IsChecked = IsChecked.HasValue && !IsChecked.Value;
        }
    }
}
