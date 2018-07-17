using OxyPlot;
using OxyPlot.Series;
using MbientLab.MetaWear;
using MbientLab.MetaWear.Core;
using MbientLab.MetaWear.Data;
using MbientLab.MetaWear.Sensor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Devices.Bluetooth;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Globalization.DateTimeFormatting;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using OxyPlot.Axes;
using MbientLab.MetaWear.Core.SensorFusionBosch;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace RealTimeGraph { //Define the plot and plot types

    public class MainViewModel
    {
        public const int MAX_DATA_SAMPLES = 960;
        public MainViewModel(){
            MyModel = new PlotModel
            { //HEADER TITLE
                Title = "Quarternion Rotation",
                IsLegendVisible = true
            };
            MyModel.Series.Add(new LineSeries   
            { //W-AXIS TITLE
                BrokenLineStyle = LineStyle.Solid,
                MarkerStroke = OxyColor.FromRgb(1, 0, 0),
                LineStyle = LineStyle.Solid,
                Title = "W-axis"
            });
            MyModel.Series.Add(new LineSeries
            { //Z-AXIS TITLE
                MarkerStroke = OxyColor.FromRgb(0, 0, 1),
                LineStyle = LineStyle.Solid,
                Title = "Z-axis"
            });
            MyModel.Axes.Add(new LinearAxis
            { //AXIS PROPERTIES
                Position = AxisPosition.Left,
                MajorGridlineStyle = LineStyle.Solid,
                AbsoluteMinimum = -1.1,
                AbsoluteMaximum = 1.1,
                Minimum = -1,
                Maximum = 1,
                Title = "Quarternion Values"
            });
            MyModel.Axes.Add(new LinearAxis
            { //HOW MANY SAMPLES TO KEEP ON SCREEN (960)
                IsPanEnabled = true,
                Position = AxisPosition.Bottom,
                MajorGridlineStyle = LineStyle.Solid,
                AbsoluteMinimum = 0,
                Minimum = 0,
                Maximum = MAX_DATA_SAMPLES
            });   
            
            MyLocal = new PlotModel
            { //HEADER TITLE
                Title = "Linear Acceleration",
                IsLegendVisible = true
            };
            MyLocal.Series.Add(new LineSeries
            { //X-AXIS TITLE
                BrokenLineStyle = LineStyle.Solid,
                MarkerStroke = OxyColor.FromRgb(1, 0, 0),
                LineStyle = LineStyle.Solid,
                Title = "X-axis"
            });
            MyLocal.Series.Add(new LineSeries
            { //Y-AXIS TITLE
                MarkerStroke = OxyColor.FromRgb(0, 1, 0),
                LineStyle = LineStyle.Solid,
                Title = "Y-axis"
            });
            MyLocal.Series.Add(new LineSeries
            { //Z-AXIS TITLE
                MarkerStroke = OxyColor.FromRgb(0, 0, 1),
                LineStyle = LineStyle.Solid,
                Title = "Z-axis"
            });
            MyLocal.Axes.Add(new LinearAxis
            { //AXIS PROPERTIES
                Position = AxisPosition.Left,
                MajorGridlineStyle = LineStyle.Solid,
                AbsoluteMinimum = -5,
                AbsoluteMaximum = 5,
                Minimum = -5,
                Maximum = 5,
                Title = "g's"
            });
            MyLocal.Axes.Add(new LinearAxis
            { //HOW MANY SAMPLES TO KEEP ON SCREEN (960)
                IsPanEnabled = true,
                Position = AxisPosition.Bottom,
                MajorGridlineStyle = LineStyle.Solid,
                AbsoluteMinimum = 0,
                Minimum = 0,
                Maximum = MAX_DATA_SAMPLES
            });
        }
        public PlotModel MyModel { get; private set; }
        public PlotModel MyLocal { get; private set; }

    }

    //public class SecondViewModel
    //{
    //    public const int MAX_DATA_SAMPLES = 960;
    //    public SecondViewModel()
    //    {
    //        MyLocal = new PlotModel
    //        { //HEADER TITLE
    //            Title = "Linear Acceleration",
    //            IsLegendVisible = true
    //        };
    //        MyLocal.Series.Add(new LineSeries
    //        { //X-AXIS TITLE
    //            BrokenLineStyle = LineStyle.Solid,
    //            MarkerStroke = OxyColor.FromRgb(1, 0, 0),
    //            LineStyle = LineStyle.Solid,
    //            Title = "X-axis"
    //        });
    //        MyLocal.Series.Add(new LineSeries
    //        { //Y-AXIS TITLE
    //            MarkerStroke = OxyColor.FromRgb(0, 1, 0),
    //            LineStyle = LineStyle.Solid,
    //            Title = "Y-axis"
    //        });
    //        MyLocal.Series.Add(new LineSeries
    //        { //Z-AXIS TITLE
    //            MarkerStroke = OxyColor.FromRgb(0, 0, 1),
    //            LineStyle = LineStyle.Solid,
    //            Title = "Z-axis"
    //        });
    //        MyLocal.Axes.Add(new LinearAxis
    //        { //AXIS PROPERTIES
    //            Position = AxisPosition.Left,
    //            MajorGridlineStyle = LineStyle.Solid,
    //            AbsoluteMinimum = -5,
    //            AbsoluteMaximum = 5,
    //            Minimum = -5,
    //            Maximum = 5,
    //            Title = "g's"
    //        });
    //        MyLocal.Axes.Add(new LinearAxis
    //        { //HOW MANY SAMPLES TO KEEP ON SCREEN (960)
    //            IsPanEnabled = true,
    //            Position = AxisPosition.Bottom,
    //            MajorGridlineStyle = LineStyle.Solid,
    //            AbsoluteMinimum = 0,
    //            Minimum = 0,
    //            Maximum = MAX_DATA_SAMPLES
    //        });
    //    }
    //    public PlotModel MyLocal { get; private set; }
    //}

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LineGraph : Page {

        //FUNCTIONS HERE
        //Write values from sensor fusion to the graph -- quarternion
        //Hopeful: make second graph that writes values from sensor fusion Lin-acc to 2nd graph

        //Re-adjust values for graph range (-1 to +1)


        private IMetaWearBoard metawear;
        //private IAccelerometer accelerometer;
        private ISensorFusionBosch sensorfusion;

       public LineGraph() {
            InitializeComponent();
        }
        

        protected async override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            var samples = 0;
            var samples2 = 0;
            var model = (DataContext as MainViewModel).MyModel;
            var model2 = (DataContext as MainViewModel).MyLocal;
            

            //Sensor Configuration
            metawear = MbientLab.MetaWear.Win10.Application.GetMetaWearBoard(e.Parameter as BluetoothLEDevice);
            sensorfusion = metawear.GetModule<ISensorFusionBosch>();
            sensorfusion.Configure(mode: Mode.Ndof, ar: AccRange._2g, gr: GyroRange._500dps);
            //-----------------------

            //await sensorfusion.Quaternion.AddRouteAsync(source => source.Stream(async data => {
            //    var value = data.Value<Quaternion>();
            //    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, () => {

            //        (model.Series[0] as LineSeries).Points.Add(new DataPoint(samples, value.W));                    
            //        (model.Series[1] as LineSeries).Points.Add(new DataPoint(samples, value.Z));
            //        samples++;                    

            //        model.InvalidatePlot(true);
            //        if (samples > MainViewModel.MAX_DATA_SAMPLES) {
            //            model.Axes[1].Reset();
            //            model.Axes[1].Maximum = samples;
            //            model.Axes[1].Minimum = (samples - MainViewModel.MAX_DATA_SAMPLES);
            //            model.Axes[1].Zoom(model.Axes[1].Minimum, model.Axes[1].Maximum);                        
            //        }
            //    });
            //}));

            //===== This is for the Linear Acceleration =====

            await sensorfusion.LinearAcceleration.AddRouteAsync(source => source.Stream(async data =>
            {
                var value2 = data.Value<CorrectedAcceleration>();
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                {
                    (model2.Series[0] as LineSeries).Points.Add(new DataPoint(samples2, value2.X));
                    (model2.Series[1] as LineSeries).Points.Add(new DataPoint(samples2, value2.Y));
                    (model2.Series[2] as LineSeries).Points.Add(new DataPoint(samples2, value2.Z));
                    samples2++;

                    model2.InvalidatePlot(true);
                    if (samples > MainViewModel.MAX_DATA_SAMPLES)
                    {
                        model2.Axes[1].Reset();
                        model2.Axes[1].Maximum = samples2;
                        model2.Axes[1].Minimum = (samples2 - MainViewModel.MAX_DATA_SAMPLES);
                        model2.Axes[1].Zoom(model2.Axes[1].Minimum, model2.Axes[1].Maximum);
                    }
                });
            }));
        }

        private async void back_Click(object sender, RoutedEventArgs e) {
            if (!metawear.InMetaBootMode) {
                metawear.TearDown();
                await metawear.GetModule<IDebug>().DisconnectAsync();
            }
            Frame.GoBack();
        }

        private void streamSwitch_Toggled(object sender, RoutedEventArgs e) {
            if (streamSwitch.IsOn) {
                sensorfusion.Quaternion.Start();
                sensorfusion.Start();
            } else {
                sensorfusion.Stop();
                sensorfusion.Quaternion.Stop();
            }
        }
    }
}

/** --  Junk Code--

    Original Code
metawear = MbientLab.MetaWear.Win10.Application.GetMetaWearBoard(e.Parameter as BluetoothLEDevice);
accelerometer = metawear.GetModule<IAccelerometer>();
accelerometer.Configure(odr: 100f, range: 8f);




















  **/
