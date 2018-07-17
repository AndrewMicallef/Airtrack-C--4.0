//###############################################################################################################################################################################
/* Name: Air Track C# UWP Program
 * Function: Recieve sensor values. Output Values.
 * Inputs: Mbientlab IMU, Arduino Due (PIXY)
 * Description: Tutorial that has been modified.
 * Last Modified: 28 June 2018
 * Developer: Leonard Lee
 */

///################################################## Libraries ###############################################################################################
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;

using MbientLab.MetaWear;
using MbientLab.MetaWear.Core;
using MbientLab.MetaWear.Core.SensorFusionBosch;
using MbientLab.MetaWear.Core.Settings;
using MbientLab.MetaWear.Data;
using MbientLab.MetaWear.Sensor;
using MbientLab.MetaWear.Peripheral;
using MbientLab.MetaWear.Peripheral.Led;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;


using Windows.ApplicationModel.Core;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Globalization.DateTimeFormatting;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.Storage.Streams;

///################################################## PROGRAM START ###############################################################################################


namespace RealTimeGraph {

    ///---------------------------------------------- OXY PLOT DEFINITION -------------------------------------------------------------------
    ///Define the plot and plot types
    public class MainViewModel
    {
        public const int MAX_DATA_SAMPLES = 10000;
        public MainViewModel(){
            MazePosModel = new PlotModel
            { //HEADER TITLE
                Title = "Maze Position",
                IsLegendVisible = true
            };
            MazePosModel.Series.Add(new LineSeries
            { //TITLE
                MarkerStroke = OxyColor.FromRgb(1, 0, 0),
                LineStyle = LineStyle.Solid,
                Title = "Current Position"
            });
            MazePosModel.Series.Add(new LineSeries
            { //TITLE
                MarkerStroke = OxyColor.FromRgb(0, 1, 0),
                LineStyle = LineStyle.Solid,
                Title = ""
            });
            MazePosModel.Axes.Add(new LinearAxis //Y Axis
            { //AXIS PROPERTIES
                Position = AxisPosition.Left,
                MajorGridlineStyle = LineStyle.Solid,
                AbsoluteMinimum = 0,
                AbsoluteMaximum = 400,
                Minimum = 0,
                Maximum = 400,
                Title = "Y Pixels"
            });
            MazePosModel.Axes.Add(new LinearAxis //X Axis
            { 
                IsPanEnabled = false,
                Position = AxisPosition.Bottom,
                MajorGridlineStyle = LineStyle.Solid,
                AbsoluteMinimum = 0,
                Minimum = 0,
                Maximum = 400,
                Title = "X Pixels"
            });
            //------------------------------------------------
            EulerModel = new PlotModel
            { //HEADER TITLE
                Title = "Euler Angles",
                IsLegendVisible = true
            };            
            EulerModel.Series.Add(new LineSeries
            { //W-AXIS TITLE
                MarkerStroke = OxyColor.FromRgb(1, 0, 0),
                LineStyle = LineStyle.Solid,
                Title = "Heading"
            });
            EulerModel.Axes.Add(new LinearAxis
            { //AXIS PROPERTIES
                Position = AxisPosition.Left,
                MajorGridlineStyle = LineStyle.Solid,
                AbsoluteMinimum = -0.1,
                AbsoluteMaximum = 360.1,
                Minimum = 0,
                Maximum = 360,
                Title = "Degrees"
            });
            EulerModel.Axes.Add(new LinearAxis
            { //HOW MANY SAMPLES TO KEEP ON SCREEN (960)
                IsPanEnabled = true,
                Position = AxisPosition.Bottom,
                MajorGridlineStyle = LineStyle.Solid,
                AbsoluteMinimum = 0,
                Minimum = 0,
                Maximum = MAX_DATA_SAMPLES
            });

        }        
        public PlotModel EulerModel { get; private set; }
        public PlotModel MazePosModel { get; private set; }

    }

    

    ///---------------------------------------------- PAGE STUFFZ -------------------------------------------------------------------
    /// An empty page that can be used on its own or navigated to within a Frame.
       
    public sealed partial class LineGraph : Page
    {
        ///------------------------------------------ GLOBAL VARIABLES --------------------------------------------------------------

        /// UI Button Global Variables 
        public bool stream_state = false;
        public bool IMU_graph_en_state = false;
        public bool folder_state = false;
        public bool PIXY_graph_en_state = false;
        public bool PIXY_graph_clear_state = false;

        /// IMU Global Variables                  
        public IMetaWearBoard metawear;        
        public ISensorFusionBosch sensorfusion;
        public ISettings settings;
        
        /// Stream Port + File Global Variables
        public string file_name;
        public Windows.Storage.StorageFolder folder;
        public Windows.Storage.StorageFolder mainFolder;
        public Windows.Storage.StorageFile file;
        public IRandomAccessStream csv_stream;

        /// CSV Data Global Variables
        public int i = 0;
        public string buffer_string = "";
        public string string1 = "";

        /// Timer Global Variables
        public double time_base_ms = 0;
        public double time_base_sec = 0;
        public double time_base_min = 0;
        public double time_base_hour = 0;
        public double base_elapsed_millisec = 0;
        public double time_current_ms = 0;
        public double time_current_sec = 0;
        public double time_current_min = 0;
        public double time_current_hour = 0;
        public double current_elapsed_millisec = 0;

        /// Stream OXY Plot Global Variables
        public double graph_time_ms = 0;
        public bool time_base_set = false;
        public float angle_maze = 0;
        public float angle_actual_maze = 0;

        /// Arduino Due Serial Port Global Variables
        public DataReader dataReader;
        public SerialDevice serialPortDue;

        /// Arduino Due PIXY Data Global Arrays Variables
        public string array1 = "";
        public string array2 = "";
        public string array3 = "0";
        public string array4 = "0";

        public int zerox = 0;
        public int zeroy = 0;
        public int arm_length = 112;
        public int arm_width = 35;

        public float zeroangle = 0;
        public int x_coor = 0;
        public int y_coor = 0;
        public int x_disp = 0;
        public int y_disp = 0;
        public volatile bool x_neg_dir = false;
        public volatile bool y_neg_dir = false;
        public int radius = 0;
        public float angle = 0;
        public float angle2 = 0;

        public double x_post = 0;
        public double y_post = 0;




        ///------------------------------------------ PAGE START FUNCTIONS ----------------------------------------------------------
        public LineGraph()
        {
            InitializeComponent();
        }


        ///------------------------------------------ NAVIGATION TO FUNCTIONS -------------------------------------------------------
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var model1 = (DataContext as MainViewModel).EulerModel;
            Draw_X_Maze_plot();

            LineGraph content = Frame.Content as LineGraph;

            //Sensor Configuration
            metawear = MbientLab.MetaWear.Win10.Application.GetMetaWearBoard(e.Parameter as BluetoothLEDevice);
            sensorfusion = metawear.GetModule<ISensorFusionBosch>();
            sensorfusion.Configure(mode: Mode.Ndof, ar: AccRange._2g, gr: GyroRange._500dps);
            ISettings settings = metawear.GetModule<ISettings>();
            settings.EditBleConnParams(maxConnInterval: 7.5f);    
            IMacro macro = metawear.GetModule<IMacro>();

            await PickFolder();         
            csv_stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);            

            await sensorfusion.EulerAngles.AddRouteAsync(source => source.Stream(async data =>
            {
                var value = data.Value<EulerAngles>(); 
                if (file != null)
                {
                    //If stream is enabled
                    if (stream_state == true)
                    {
                        saveToCSV(data);      
                        if (time_base_set == false)
                        {
                            time_base_ms = data.Timestamp.Millisecond;    
                            time_base_sec = data.Timestamp.Second;
                            time_base_min = data.Timestamp.Minute;
                            time_base_hour = data.Timestamp.Hour;
                            base_elapsed_millisec = time_base_ms + (1000 * time_base_sec) + (1000 * 60 * time_base_min) + (1000 * 60 * 60 * time_base_hour);
                            time_base_set = true;
                        }

                        time_current_ms = data.Timestamp.Millisecond;
                        time_current_sec = data.Timestamp.Second;
                        time_current_min = data.Timestamp.Minute;
                        time_current_hour = data.Timestamp.Hour;
                        current_elapsed_millisec = time_current_ms + (1000 * time_current_sec) + (1000 * 60 * time_current_min) + (1000 * 60 * 60 * time_current_hour);
                        graph_time_ms = current_elapsed_millisec - base_elapsed_millisec;

                        angle_maze = value.Heading;

                        CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,() =>
                        {
                            content.dataeuler_box.Text = value.Heading.ToString() + "°";
                        });

                        if (IMU_graph_en_state == true)
                        {
                            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                            {
                                (model1.Series[0] as LineSeries).Points.Add(new DataPoint(graph_time_ms, value.Heading));   
                                model1.InvalidatePlot(true);
                                if (graph_time_ms > MainViewModel.MAX_DATA_SAMPLES)
                                {
                                    model1.Axes[1].Reset();
                                    model1.Axes[1].Maximum = graph_time_ms;
                                    model1.Axes[1].Minimum = (graph_time_ms - MainViewModel.MAX_DATA_SAMPLES);
                                    model1.Axes[1].Zoom(model1.Axes[1].Minimum, model1.Axes[1].Maximum);
                                }
                            });
                        }
                    }
                }

                else
                {
                    //Do nothing if file is null.
                }
            }));
        }


        
        
        
        ///------------------------------------------ READ DATA FROM IMU FUNCTIONS -------------------------------------------------------
        public async void readData(IData data)
        {
            var buffer = data.Value<EulerAngles>();
            System.Diagnostics.Debug.WriteLine(buffer);
        }

        
        
        
        
        ///------------------------------------------ SAVE IMU DATA TO STREAM -------------------------------------------------------------
        public async void saveToCSV(IData data)
        {

            //var data_entry = string.Format("{0}", data.ToString());
            var data_quaternion = data.Value<EulerAngles>();
            var data_q_ts = data.FormattedTimestamp;
            var data_raw = string.Format("{0}", data.ToString() + "\r\n");
            var data_q_H = data_quaternion.Heading;
            var data_q_Y = data_quaternion.Yaw;            
            
            using (var outputStream = csv_stream.GetOutputStreamAt(csv_stream.Size))
            {
                using (var dataWriter = new Windows.Storage.Streams.DataWriter(outputStream))
                {
                    string1 = data_q_ts + "," + data_q_H + "\r\n";
                    if (i < 5000)
                    {
                        buffer_string = buffer_string + string1;
                        i++;
                    }

                    else
                    {
                        buffer_string = buffer_string + string1;
                        dataWriter.WriteString(buffer_string);
                        await dataWriter.StoreAsync();
                        await outputStream.FlushAsync();
                        buffer_string = ""; //flush varaible
                        i = 0; //reset counter
                    }                    
                }                
            }
        }
        
        
        
        
        ///------------------------------------------ UI FOLDER SELECTION FUNCTION -------------------------------------------------------
        public async Task PickFolder()
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add(".csv");
            folderPicker.FileTypeFilter.Add(".txt");

            while (folder == null)
            {
                folder = await folderPicker.PickSingleFolderAsync();
            }            

            if (folder != null)
            {
                // Application now has read/write access to all contents in the picked folder (including other sub-folder contents)
                string current_time = DateTime.Now.ToString("dd-MM-yyyy_hh-mm-ss");
                file_name = "IMU_Sensor_" + current_time + ".csv";
                Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder); 
                mainFolder = await folder.CreateFolderAsync("Movement_Data", CreationCollisionOption.OpenIfExists);        
                file = await mainFolder.CreateFileAsync(file_name, CreationCollisionOption.GenerateUniqueName);
                List<string> lines = new List<string>() { "TimeStamps , Heading"}; 
                await FileIO.WriteLinesAsync(file, lines);
            }

            else
            {
                // the user didn't select any folder
            }
        }

        ///------------------------------------------ Arduino Due PIXY Initialization FUNCTION -----------------------------------------
        public async void InitializeConnection()
        {
            var aqs = SerialDevice.GetDeviceSelectorFromUsbVidPid(0x2341, 0x003D); //0x2A03, 0x0057 (Uno On Table = 0x2341, 0x0043)
            var info = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(aqs);

            // Get connection data
            serialPortDue = await SerialDevice.FromIdAsync(info[0].Id);

            // Configure serial settings
            serialPortDue.DataBits = 8;
            serialPortDue.BaudRate = 115200;
            serialPortDue.Parity = SerialParity.None;
            serialPortDue.Handshake = SerialHandshake.None;
            serialPortDue.StopBits = SerialStopBitCount.One;
            serialPortDue.ReadTimeout = TimeSpan.FromMilliseconds(1000);
            serialPortDue.WriteTimeout = TimeSpan.FromMilliseconds(1000);
            dataReader = new DataReader(serialPortDue.InputStream);
        }




        ///------------------------------------------ Arduino Due PIXY Read data FUNCTION ----------------------------------------------
        private async Task ReadAsync()
        {
            /* Active Method:
                 * 1. Read array1 1 byte
                 * 2. Read value
                 * 2a. If number, write to array2
                 * 2b. If " ", array3 = array2. Clear array2
                 * 2c. If " ", array4 = array2. Clear array2
                 */

            try
            {
                Task<uint> loadAsyncTask;
                dataReader.ByteOrder = ByteOrder.BigEndian;
                dataReader.InputStreamOptions = InputStreamOptions.ReadAhead;
                dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
                uint readBufferLength = 1;

                loadAsyncTask = dataReader.LoadAsync(readBufferLength).AsTask();
                uint ReadAsyncBytes = await loadAsyncTask;

                if (ReadAsyncBytes > 0)
                {
                    string data = dataReader.ReadString(readBufferLength);
                    //System.Diagnostics.Debug.Write(data);
                    array1 = data;
                }                

                if (array1[0] == (' '))
                {
                    array3 = array2;                    
                    array2 = ""; //
                }

                else if (array1[0] == ('\r'))
                {
                    array4 = array2;
                    array2 = ""; //<-- write array3 function here to the graph
                    oxyplot_MazePosModel();
                }

                else if (array1[0] == ('\n'))
                {
                    //do nothing
                }

                else
                {
                    array2 += array1[0];
                }
                
                datax_box.Text = array3;
                datay_box.Text = array4;
            }

            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);                
            }
        }


        ///------------------------------------------ UI Based Arduino Read Enable Function ---------------------------------------------
        public async void Read_Serial_data()
        {
            LineGraph content = Frame.Content as LineGraph;
            while (arduino_readSwitch.IsOn)
            {
                await ReadAsync();
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    content.datax_box.Text = array3;
                    content.datay_box.Text = array4;
                    content.dataxzero_box.Text = zerox.ToString();
                    content.datayzero_box.Text = zeroy.ToString();
                    content.dataanglezero_box.Text = zeroangle.ToString();
                    content.dataxdisp_box.Text = x_disp.ToString();
                    content.dataydisp_box.Text = y_disp.ToString();
                    content.dataangleactual_box.Text = angle_actual_maze.ToString();
                    content.dataradiusdisp_box.Text = radius.ToString();
                    content.dataAngledisp_box.Text = angle.ToString();
                    content.dataAngle2disp_box.Text = angle2.ToString();
                    content.dataxpost_box.Text = x_post.ToString();
                    content.dataypost_box.Text = y_post.ToString(); 
                });
            }
        }

        

            ///------------------------------------------ Write PIXY Data to Graph ---------------------------------------------------------
            public async void oxyplot_MazePosModel()
        {
            //double unit_circle_x = 0;
            //double unit_circle_y = 0;            

            double oxyplot_x = 0;
            double oxyplot_y = 0;

            /* Input variables:
             * array3 = x string
             * array4 = y string
             * angle_maze = raw maze angle
             * zerox
             * zeroy
             * zeroangle
             * angle_actual_maze = actual angle after calibration
             */


            //Gather PIXY Raw Coordinates
            var model2 = (DataContext as MainViewModel).MazePosModel;
            Int32.TryParse(array3, out x_coor);            
            Int32.TryParse(array4, out y_coor);
            
            //Calibrate the rotation of the IMU sensor
            //measured(150) - zero point(30), actual = 120. If measured >= zero point, actual = measured - zero point
            //If measured (10) - zero (30). Else if measured < zero point, 360 - (zero point - measured)
            //If value is less than 360, thats fine
            //if value is more than 720,
            
            //Calculate calibrated angle
            if(angle_maze >= zeroangle)
            {
                angle_actual_maze = angle_maze - zeroangle;
            }

            else if(angle_maze < zeroangle)
            {
                angle_actual_maze = 360 - (zeroangle - angle_maze);
            }
            

            //double radius = Math.Sqrt((x_coor * x_coor) + (y_coor * y_coor))/2;
            //double angle = Convert.ToDouble(angle_maze);
            //unit_circle_x =  150 + (radius * Math.Cos(angle_maze));
            //unit_circle_y =  (radius * Math.Sin(angle_maze));

            // =============== Calculate X and Y Displacement
            

            x_disp = x_coor - zerox;
            y_disp = y_coor - zeroy;


            // =============== Calculate oxy_plot X and Y








            


            if (PIXY_graph_en_state == true && PIXY_graph_clear_state == false)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                {
                    if(oxyplot_x > 0 && oxyplot_y > 0)
                    {
                        (model2.Series[0] as LineSeries).Points.Add(new DataPoint(oxyplot_x, oxyplot_y));
                        model2.InvalidatePlot(true);
                    }
                    
                    
                });
            }
            
        }

        ///------------------------------------------ UI ACTION BUTTON FUNCTIONS -------------------------------------------------------

        private void streamSwitch_Toggled(object sender, RoutedEventArgs e) {
            if (streamSwitch.IsOn) {
                stream_state = true;
                sensorfusion.EulerAngles.Start();                
                sensorfusion.Start();
            } else {
                sensorfusion.Stop();
                sensorfusion.EulerAngles.Stop();
            }
        }

        private void IMU_graph_en_Toggled(object sender, RoutedEventArgs e)
        {
            if (IMU_graph_en.IsOn)
            {
                IMU_graph_en_state = true;
            }

            else
            {
                IMU_graph_en_state = false;
            }
        }

        private void arduino_initSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (arduino_initSwitch.IsOn)
            {                
                InitializeConnection();                
            }
            else
            {                
                //Do Nothing
            }
        }

        private void arduino_readSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (arduino_readSwitch.IsOn)
            {                
                Read_Serial_data();
            }

            else
            {
                //Do Nothing
            }
        }

        private void PIXY_graph_en_Toggled(object sender, RoutedEventArgs e)
        {
            if (PIXY_graph_en.IsOn)
            {
                PIXY_graph_en_state = true;
            }

            else
            {
                PIXY_graph_en_state = false;
            }
        }

        private void zero_set_Click(object sender, RoutedEventArgs e)
        {
            zerox = x_coor;
            zeroy = y_coor;
            zeroangle = angle_maze;
        }

        private void graph_clear_Click(object sender, RoutedEventArgs e)
        {
            //Extend scope so model2 can be seen
            var model2 = (DataContext as MainViewModel).MazePosModel;

            //Disable the graph
            PIXY_graph_clear_state = true;

            //Reset Everything?
            model2.Axes[0].Reset();
            model2.Axes[1].Reset();
            model2.Series.Clear();
            model2.ResetAllAxes();
            model2.InvalidatePlot(true);

            //Reintialise graph properties
            model2.Series.Add(new LineSeries
            { //W-AXIS TITLE
                MarkerStroke = OxyColor.FromRgb(1, 0, 0),
                LineStyle = LineStyle.Solid,
                Title = "Current Position"
            });
            model2.Series.Add(new LineSeries
            { //W-AXIS TITLE
                MarkerStroke = OxyColor.FromRgb(0, 1, 0),
                LineStyle = LineStyle.Solid,
                Title = ""
            });
            model2.Axes.Add(new LinearAxis //Y Axis
            { //AXIS PROPERTIES
                Position = AxisPosition.Left,
                MajorGridlineStyle = LineStyle.Solid,
                AbsoluteMinimum = 0,
                AbsoluteMaximum = 400,
                Minimum = 0,
                Maximum = 400,
                Title = "Y Pixels"
            });
            model2.Axes.Add(new LinearAxis //X Axis
            {
                IsPanEnabled = false,
                Position = AxisPosition.Bottom,
                MajorGridlineStyle = LineStyle.Solid,
                AbsoluteMinimum = 0,
                Minimum = 0,
                Maximum = 400,
                Title = "X Pixels"
            });

            //Draw Graph
            Draw_X_Maze_plot();

            //Re-enable graph
            PIXY_graph_clear_state = false;            

        }

        private async Task Draw_X_Maze_plot()
        {
            var model2 = (DataContext as MainViewModel).MazePosModel;
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                (model2.Series[1] as LineSeries).Points.Add(new DataPoint(200  - arm_width / 2, 200  + arm_width / 2)); //A
                (model2.Series[1] as LineSeries).Points.Add(new DataPoint(200  - arm_width / 2, 200  + arm_length)); //B
                (model2.Series[1] as LineSeries).Points.Add(new DataPoint(200  + arm_width / 2, 200  + arm_length)); //C
                (model2.Series[1] as LineSeries).Points.Add(new DataPoint(200  + arm_width / 2, 200  + arm_width / 2)); //D                
                (model2.Series[1] as LineSeries).Points.Add(new DataPoint(200  + arm_length, 200  + arm_width / 2)); //E
                (model2.Series[1] as LineSeries).Points.Add(new DataPoint(200  + arm_length, 200 - arm_width / 2)); //F
                (model2.Series[1] as LineSeries).Points.Add(new DataPoint(200  + arm_width / 2, 200  - arm_width / 2)); //G
                (model2.Series[1] as LineSeries).Points.Add(new DataPoint(200  + arm_width / 2, 200  - arm_length)); //H                
                (model2.Series[1] as LineSeries).Points.Add(new DataPoint(200  - arm_width / 2, 200  - arm_length)); //I
                (model2.Series[1] as LineSeries).Points.Add(new DataPoint(200  - arm_width / 2, 200  - arm_width / 2)); //J
                (model2.Series[1] as LineSeries).Points.Add(new DataPoint(200  - arm_length, 200  - arm_width / 2)); //K
                (model2.Series[1] as LineSeries).Points.Add(new DataPoint(200  - arm_length, 200  + arm_width / 2)); //L
                (model2.Series[1] as LineSeries).Points.Add(new DataPoint(200 - arm_width / 2, 200 + arm_width / 2)); //A
                model2.InvalidatePlot(true);
            });
        }

        private void TextBlock_SelectionChanged(object sender, RoutedEventArgs e)
        {
            //Where did this come from?
        }

        private async void back_Click(object sender, RoutedEventArgs e)
        {
            if (!metawear.InMetaBootMode)
            {
                metawear.TearDown();
                await metawear.GetModule<IDebug>().DisconnectAsync();
            }
            Frame.GoBack();
        }

        
    }
}










































/** --  Junk Code--

    Original Code

metawear = MbientLab.MetaWear.Win10.Application.GetMetaWearBoard(e.Parameter as BluetoothLEDevice);
accelerometer = metawear.GetModule<IAccelerometer>();
accelerometer.Configure(odr: 100f, range: 8f);


     //var csv = new StringBuilder(data_entry);
            //csv.AppendLine(Environment.NewLine);
            //string pathString = System.IO.Path.Combine(@"C:\airtrack_csv", "data.csv");
            // File.AppendAllText("data.csv", csv.ToString());

            //await Windows.Storage.FileIO.WriteTextAsync(MyFile, data_entry + Environment.NewLine);
            //await Windows.Storage.FileIO.WriteTextAsync(usedFile, Enviro);

            //using (IRandomAccessStream textStream = await MyFile.OpenAsync(FileAccessMode.ReadWrite))
            //{
            //    using (DataWriter textWriter = new DataWriter(textStream))
            //    {
            //        textWriter.WriteString(data_entry + Environment.NewLine);
            //        await textWriter.StoreAsync();
            //    }
            //}

            //var stream = await MyFile.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);
            //using (var outputStream = stream.GetOutputStreamAt(stream.Size))
            //{
            //    using (var dataWriter = new Windows.Storage.Streams.DataWriter(outputStream))
            //    {
            //        dataWriter.WriteString(data_raw + "\r\n" );
            //        await dataWriter.StoreAsync();
            //        await outputStream.FlushAsync();
            //    }
            //}
            // stream.Dispose(); // Or use the stream variable (see previous code snippet) with a using statement as well.

            //System.IO.File.AppendAllText(StorageFile MyFile, "text content");

            //await Windows.Storage.FileIO.AppendTextAsync(MyFile, data_raw);



     _readWriteLock.EnterWriteLock();
            try
            {
                using (var outputStream = stream.GetOutputStreamAt(stream.Size))
                {
                    using (var dataWriter = new Windows.Storage.Streams.DataWriter(outputStream))
                    {
                        dataWriter.WriteString(data_q_ts + "," + data_q_W + "," + data_q_Z + "\r\n");
                        await dataWriter.StoreAsync();
                        await outputStream.FlushAsync();
                    }
                }
                stream.Dispose(); // Or use the stream variable (see previous code snippet) with a using statement as well.                
            }

            finally
            {
                _readWriteLock.ExitWriteLock();
            }



             //settings.OnDisconnectAsync(() => {
            //    ILed led = metawear.GetModule<ILed>();
            //    led.EditPattern(Color.Red, Pattern.Solid, count: 2);
            //    led.Play();
            //});

            //MbientLab.MetaWear.IMetaWearBoard.ReadBatteryLevelAsync()

            //====== Creating .CSV Folder for data to be stored           
            //StorageFolder newFolder = await DownloadsFolder.CreateFolderAsync("--Mbientlab Data-- ");
            //StorageFile newFile = await DownloadsFolder.CreateFileAsync("test.txt");

            //StorageFolder MyFolder = await DownloadsFolder.CreateFolderAsync("-- AIRTRACK IMU Data --", CreationCollisionOption.OpenIfExists);
            //StorageFile MyFile = await MyFolder.CreateFileAsync("Movement_data.csv", CreationCollisionOption.GenerateUniqueName);

            //StorageFolder MyFolder = await DownloadsFolder.CreateFolderAsync("AIRTRACK_IMU_Data_2", CreationCollisionOption.OpenIfExists);
            //StorageFile MyFile = await MyFolder.CreateFileAsync("Movement_data.csv", CreationCollisionOption.OpenIfExists);
            
            //-----------------------

            //await sensorfusion.LinearAcceleration.AddRouteAsync(source => source.Stream(async data =>            
            //{                
            //    var value = data.Value<Acceleration>();
            //    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            //    {                    
            //        (model.Series[0] as LineSeries).Points.Add(new DataPoint(samples, value.X));
            //        (model.Series[1] as LineSeries).Points.Add(new DataPoint(samples, value.Y));
            //        (model.Series[2] as LineSeries).Points.Add(new DataPoint(samples, value.Z));
            //        samples++;  
            //        model.InvalidatePlot(true);
            //        if (samples > MainViewModel.MAX_DATA_SAMPLES)
            //        {
            //            model.Axes[1].Reset();
            //            model.Axes[1].Maximum = samples;
            //            model.Axes[1].Minimum = (samples - MainViewModel.MAX_DATA_SAMPLES);
            //            model.Axes[1].Zoom(model.Axes[1].Minimum, model.Axes[1].Maximum);
            //        }
            //    });
            //}));




    // Set Status to Locked           
    ISSUE WITH THIS IS OPENING AND WRITING AND CLOSING IS RESOURCE HEAVY. NEED TO STREAM. 
            List<string> data_line = new List<string>() { data_q_ts + "," + data_q_W + "," + data_q_Z };
            FileIO.AppendLinesAsync(file, data_line);     




    //string path = @"C:\Users\lab-admin\AppData\Local\Packages\fcbddcdb-d674-485d-b099-9550c89131c5_wkrz0q4h1x9v2\Movement_data_2.csv";

            //var myFile = File.Create(path);
            //myFile.Close();

            //Use stream to write to the file
            //StorageFolder MyFolder = ApplicationData.Current.LocalFolder; //await DownloadsFolder.CreateFolderAsync("AIRTRACK_IMU_Data_2");
            //StorageFile MyFile = await MyFolder.CreateFileAsync("Movement_data_2.csv", CreationCollisionOption.OpenIfExists);
            //var stream = await MyFile.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite, StorageOpenOptions.AllowReadersAndWriters);

            // Set Status to Locked            
            //List<string> data_line = new List<string>() { data_q_ts + "," + data_q_W + "," + data_q_Z };
            //FileIO.AppendLinesAsync(file, data_line);

            //var stream = file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);
            //using (var outputStream = stream.GetOutputStreamAt(stream.Size))
            //{
            //    using (var dataWriter = new Windows.Storage.Streams.DataWriter(outputStream))
            //    {
            //        dataWriter.WriteString(data_raw + "\r\n");
            //        dataWriter.StoreAsync();
            //        outputStream.FlushAsync();
            //    }
            //}

            
                               
    
    
    
    //readData(data);
                    
                    //stopwatch.Stop();

                    //Running Average
                    //rate_cnt_5 = rate_cnt_4;
                    //rate_cnt_4 = rate_cnt_3;
                    //rate_cnt_3 = rate_cnt_2;
                    //rate_cnt_2 = rate_cnt_1;
                    //rate_cnt_1 = (int)stopwatch.ElapsedMilliseconds;
                    //rate_cnt_avg = (rate_cnt_1 + rate_cnt_2 + rate_cnt_3 + rate_cnt_4 + rate_cnt_5) / 5;

                    //if (rate_cnt_avg == 0)
                    //{
                    //    rate_cnt_hz = 0;
                    //}
                    //else if (rate_cnt_avg > 0)
                    //{
                    //    rate_cnt_hz = 1000 / rate_cnt_avg;
                    //}




    //X Y DEGREE Processing

    //if (x_disp < 1)
            //{
            //    if (x_neg_dir == false)
            //    {
            //        //oxyplot_x = 200 + x_disp;
            //    }

            //    else if (x_neg_dir == true)
            //    {
            //        //oxyplot_x = 200 - x_disp;
            //    }

            //    if (y_neg_dir == false)
            //    {
            //        //oxyplot_y = 200 + y_disp;
            //    }

            //    else if (y_neg_dir == true)
            //    {
            //        //oxyplot_y = 200 - y_disp;
            //    }
            //}

            //else
            //{
            //    if ((angle_actual_maze >= 0 && angle_actual_maze <= 30) || (angle_actual_maze >= 330 && angle_actual_maze <= 360))
            //    {       
            //        oxyplot_x = 200 - x_disp;
            //        oxyplot_y = 200 - y_disp;
            //    }

            //    else if (angle_actual_maze >= 60 && angle_actual_maze <= 120)
            //    {
            //        oxyplot_x = 200 - y_disp;
            //        oxyplot_y = 200 + x_disp;
            //    }

            //    else if (angle_actual_maze >= 150 && angle_actual_maze <= 210)
            //    {
            //        oxyplot_x = 200 + x_disp;
            //        oxyplot_y = 200 + y_disp;
            //    }

            //    else if (angle_actual_maze >= 240 && angle_actual_maze <= 300)
            //    {
            //        oxyplot_x = 200 + y_disp;
            //        oxyplot_y = 200 - x_disp;
            //    }                
            //}


            /* Proper way that doesn't work that involves caluclating each thing individually

            radius = (int)Math.Sqrt((x_disp * x_disp) + (y_disp * y_disp));
           

            if(y_disp != 0 && x_disp != 0)
            {
                angle = (float)Math.Tanh(y_disp / x_disp);
            }
            
            if (y_disp < 0)
            {
                if (angle_actual_maze + angle > 360)
                {
                    angle2 = angle_actual_maze + angle - 360;
                }

                else
                {
                    angle2 = angle_actual_maze + angle;
                }
            }

            else
            {
                if (angle_actual_maze - angle < 0)
                {
                    angle2 = 360 - angle - angle_actual_maze;
                }

                else
                {
                    angle2 = angle_actual_maze - angle;
                }
            }

            x_post = (radius * Math.Cos(angle2));
            y_post = (radius * Math.Sin(angle2));

            oxyplot_x = 200 + x_post;
            oxyplot_y = 200 + y_post;


            










  **/
