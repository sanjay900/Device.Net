﻿using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using System;
using Device.Net;
using Microsoft.Extensions.Logging;
using Device.Net.Reactive;

#if WINDOWS_UWP
using Hid.Net.UWP;
#else
using Android.Hardware.Usb;
using Android.Content;
#endif

#pragma warning disable CA2000 // Dispose objects before losing scope
#pragma warning disable IDE0022 // Use expression body for methods
#pragma warning disable CA1305 // Specify IFormatProvider

namespace UnoCrossPlatform
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
#if !WINDOWS_UWP
        public static UsbManager? UsbManager { get; set; }
        public static Context? AppContext { get; set; }
#endif

        public MainPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var loggerFactory = LoggerFactory.Create((builder) =>
             {
                 _ = builder.AddDebug().SetMinimumLevel(LogLevel.Trace);
             });

            var deviceDataStreamer =
            new FilterDeviceDefinition { VendorId = 0x413d, ProductId = 0x2107, UsagePage = 65280 }
#if WINDOWS_UWP
                .CreateUwpHidDeviceFactory(loggerFactory).ToDeviceManager(loggerFactory)
#else
                .CreateAndroidUsbDeviceFactory(UsbManager, AppContext, loggerFactory).ToDeviceManager(loggerFactory)
#endif
                .CreateDeviceDataStreamer(async (device) =>
                {
                    var data = await device.WriteAndReadAsync(new byte[9] { 0x00, 0x01, 0x80, 0x33, 0x01, 0x00, 0x00, 0x00, 0x00 });

                    var temperatureTimesOneHundred = (data.Data[4] & 0xFF) + (data.Data[3] << 8);

                    var temperatureCelsius = Math.Round(temperatureTimesOneHundred / 100.0m, 2, MidpointRounding.ToEven);

                    _ = DispatchingExtensions.RunOnDispatcher(() =>
                    {
                        TheTextBlock.Text = temperatureCelsius.ToString() + "°C";
                    });

                }).Start();

            base.OnNavigatedTo(e);
        }
    }
}
#pragma warning restore CA2000 // Dispose objects before losing scope
#pragma warning restore IDE0022 // Use expression body for methods
#pragma warning restore CA1305 // Specify IFormatProvider
