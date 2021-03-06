﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Wbooru.Models.SettingUI;
using Wbooru.Settings;
using Wbooru.Settings.UIAttributes;
using Wbooru.UI.ValueConverters.SettingUI;

namespace Wbooru.UI.Controls.SettingUI
{
    /// <summary>
    /// RangeValueSetting.xaml 的交互逻辑
    /// </summary>
    public partial class RangeValueSetting : UserControl
    {
        public PropertyInfoWrapper Wrapper { get; }

        // Using a DependencyProperty as the backing store for ProxyValue.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ProxyValueProperty =
            DependencyProperty.Register("ProxyValue", typeof(double), typeof(RangeValueSetting), new PropertyMetadata(0.0d,(d,e)=> {
                var v = TypeDescriptor.GetConverter((d as RangeValueSetting).Wrapper.PropertyInfo.PropertyType).ConvertFrom(e.NewValue.ToString());
                (d as RangeValueSetting).Wrapper.ProxyValue = v;
            }));

        public RangeValueSetting()
        {
            InitializeComponent();
        }

        public RangeValueSetting(PropertyInfoWrapper wrapper)
        {
            if (!(wrapper.PropertyInfo.GetCustomAttribute<RangeAttribute>() is RangeAttribute range))
                throw new Exception("RangeValueSetting钦定设置属性必须有[Range]特性标识");

            InitializeComponent();

            ValueRangeSlider.Value = double.Parse(wrapper.ProxyValue.ToString());
            ValueRangeSlider.Minimum = double.Parse(range.Min);
            ValueRangeSlider.Maximum = double.Parse(range.Max);

            //确保slider的value和设置钦定的值一样
            if (!(wrapper.PropertyInfo.PropertyType.Name == "Single" || wrapper.PropertyInfo.PropertyType.Name == "Double"))
            {
                ValueRangeSlider.IsSnapToTickEnabled = true;
                ValueRangeSlider.TickFrequency = 1;
            }

            if (wrapper.PropertyInfo.GetCustomAttribute<Settings.UIAttributes.DescriptionAttribute>() is Settings.UIAttributes.DescriptionAttribute description)
                NameBlock.ToolTip = description.Description;

            NameBlock.Text = wrapper.DisplayPropertyName;
            Wrapper = wrapper;

            #region Binding Chain

            Binding T2SBinding = new Binding();
            T2SBinding.Mode = BindingMode.TwoWay;
            T2SBinding.Source = ValueRangeSlider;
            T2SBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            T2SBinding.Path = new PropertyPath("Value");

            Input.SetBinding(TextBox.TextProperty, T2SBinding);

            Binding T2PBinding = new Binding();
            T2PBinding.Mode = BindingMode.TwoWay;
            T2PBinding.Source = ValueRangeSlider;
            T2PBinding.Path = new PropertyPath("Value");

            SetBinding(ProxyValueProperty, T2PBinding);

            #endregion
        }
    }
}
