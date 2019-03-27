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
using Windows.UI.Xaml.Media;
using Windows.UI;

namespace RealTimeGraph
{
    partial class LineGraph
    {
        ///------------------------------------------ Write Maze X to Graph ---------------------------------------------------------
        private async Task Draw_X_Outline_plot()
        {
            var model2 = (DataContext as MainViewModel).MazePosModel;
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                (model2.Series[1] as LineSeries).Points.Add(new DataPoint(200 - arm_width / 2, 200 + arm_width / 2)); //A
                (model2.Series[1] as LineSeries).Points.Add(new DataPoint(200 - arm_width / 2, 200 + arm_length)); //B
                (model2.Series[1] as LineSeries).Points.Add(new DataPoint(200 + arm_width / 2, 200 + arm_length)); //C
                (model2.Series[1] as LineSeries).Points.Add(new DataPoint(200 + arm_width / 2, 200 + arm_width / 2)); //D                
                (model2.Series[1] as LineSeries).Points.Add(new DataPoint(200 + arm_length, 200 + arm_width / 2)); //E
                (model2.Series[1] as LineSeries).Points.Add(new DataPoint(200 + arm_length, 200 - arm_width / 2)); //F
                (model2.Series[1] as LineSeries).Points.Add(new DataPoint(200 + arm_width / 2, 200 - arm_width / 2)); //G
                (model2.Series[1] as LineSeries).Points.Add(new DataPoint(200 + arm_width / 2, 200 - arm_length)); //H                
                (model2.Series[1] as LineSeries).Points.Add(new DataPoint(200 - arm_width / 2, 200 - arm_length)); //I
                (model2.Series[1] as LineSeries).Points.Add(new DataPoint(200 - arm_width / 2, 200 - arm_width / 2)); //J
                (model2.Series[1] as LineSeries).Points.Add(new DataPoint(200 - arm_length, 200 - arm_width / 2)); //K
                (model2.Series[1] as LineSeries).Points.Add(new DataPoint(200 - arm_length, 200 + arm_width / 2)); //L
                (model2.Series[1] as LineSeries).Points.Add(new DataPoint(200 - arm_width / 2, 200 + arm_width / 2)); //A                

                //Maze Plot icon Low
                (model2.Series[2] as LineSeries).Points.Add(new DataPoint(200 - arm_length + 20, 200 - arm_width / 2 - 20));
                (model2.Series[2] as LineSeries).Points.Add(new DataPoint(200 - arm_length + 22, 200 - arm_width / 2 - 20));
                (model2.Series[2] as LineSeries).Points.Add(new DataPoint(200 - arm_length + 22, 200 - arm_width / 2 - 22));
                (model2.Series[2] as LineSeries).Points.Add(new DataPoint(200 - arm_length + 20, 200 - arm_width / 2 - 22));
                (model2.Series[2] as LineSeries).Points.Add(new DataPoint(200 - arm_length + 20, 200 - arm_width / 2 - 20));

                //Maze Plot icon high
                (model2.Series[3] as LineSeries).Points.Add(new DataPoint(200 - arm_length + 20, 200 + arm_width / 2 + 20));
                (model2.Series[3] as LineSeries).Points.Add(new DataPoint(200 - arm_length + 22, 200 + arm_width / 2 + 20));
                (model2.Series[3] as LineSeries).Points.Add(new DataPoint(200 - arm_length + 22, 200 + arm_width / 2 + 22));
                (model2.Series[3] as LineSeries).Points.Add(new DataPoint(200 - arm_length + 20, 200 + arm_width / 2 + 22));
                (model2.Series[3] as LineSeries).Points.Add(new DataPoint(200 - arm_length + 20, 200 + arm_width / 2 + 20));

                model2.InvalidatePlot(true);
            });

            func_x_outline_drawn_success = true;
        }



        


        ///------------------------------------------ Clear Oxyplot Values ---------------------------------------------------------        
        public async Task oxyplot_clear()
        {
            try
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
                model2.Series.Add(new LineSeries //maze plot data
                { //W-AXIS TITLE
                    MarkerStroke = OxyColor.FromRgb(0, 1, 0),
                    LineStyle = LineStyle.Solid,
                    Title = ""
                });
                model2.Series.Add(new LineSeries //maze plot icon low
                { //TITLE
                    MarkerStroke = OxyColor.FromRgb(0, 1, 0),
                    LineStyle = LineStyle.Solid,
                    Title = ""
                });
                model2.Series.Add(new LineSeries //maze plot icon high
                { //TITLE
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
                Draw_X_Outline_plot();

                //Re-enable graph
                PIXY_graph_clear_state = false;

                func_oxy_clear_success = true;
            }

            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("oxyplot clear error" + ex.Message);
            }

        }






        
        ///------------------------------------------ Oxy Plot Euler update FUNCTION -------------------------------------------------------
        public void oxyplot_update_euler()
        {
            var model1 = (DataContext as MainViewModel).EulerModel;
            (model1.Series[0] as LineSeries).Points.Add(new DataPoint(trial_time_ms, angle_maze));
            model1.InvalidatePlot(true);
            if (trial_time_ms > MainViewModel.MAX_DATA_SAMPLES)
            {
                model1.Axes[1].Reset();
                model1.Axes[1].Maximum = trial_time_ms;
                model1.Axes[1].Minimum = (trial_time_ms - MainViewModel.MAX_DATA_SAMPLES);
                model1.Axes[1].Zoom(model1.Axes[1].Minimum, model1.Axes[1].Maximum);
            }
        }







        ///------------------------------------------ Oxy Plot PIXY update FUNCTION -------------------------------------------------------
        public void oxyplot_update_PIXY(bool draw_en)
        {
            var model2 = (DataContext as MainViewModel).MazePosModel;
            if(draw_en == true)
            {
                oxyplot_within_boundaries = false;

                if (plot_mode_hist_en == false)
                {
                    //Delete previous dot at old coord
                    (model2.Series[0] as LineSeries).Points.Remove(new DataPoint(last_oxyplot_x, last_oxyplot_y));
                    (model2.Series[0] as LineSeries).Points.Remove(new DataPoint(last_oxyplot_x, last_oxyplot_y + 1));
                    (model2.Series[0] as LineSeries).Points.Remove(new DataPoint(last_oxyplot_x + 1, last_oxyplot_y + 1));
                    (model2.Series[0] as LineSeries).Points.Remove(new DataPoint(last_oxyplot_x + 1, last_oxyplot_y - 1));
                    (model2.Series[0] as LineSeries).Points.Remove(new DataPoint(last_oxyplot_x - 1, last_oxyplot_y - 1));
                    (model2.Series[0] as LineSeries).Points.Remove(new DataPoint(last_oxyplot_x - 1, last_oxyplot_y + 1));
                    (model2.Series[0] as LineSeries).Points.Remove(new DataPoint(last_oxyplot_x, last_oxyplot_y + 1));

                    //Add dot at new coord
                    (model2.Series[0] as LineSeries).Points.Add(new DataPoint(oxyplot_x, oxyplot_y));
                    (model2.Series[0] as LineSeries).Points.Add(new DataPoint(oxyplot_x, oxyplot_y + 1));
                    (model2.Series[0] as LineSeries).Points.Add(new DataPoint(oxyplot_x + 1, oxyplot_y + 1));
                    (model2.Series[0] as LineSeries).Points.Add(new DataPoint(oxyplot_x + 1, oxyplot_y - 1));
                    (model2.Series[0] as LineSeries).Points.Add(new DataPoint(oxyplot_x - 1, processed_y - 1));
                    (model2.Series[0] as LineSeries).Points.Add(new DataPoint(oxyplot_x - 1, oxyplot_y + 1));
                    (model2.Series[0] as LineSeries).Points.Add(new DataPoint(oxyplot_x, oxyplot_y + 1));

                    //Update
                    model2.InvalidatePlot(true);

                    //keep buffer of previous values;
                    last_oxyplot_x = oxyplot_x;
                    last_oxyplot_y = oxyplot_y;
                }

                else
                {
                    (model2.Series[0] as LineSeries).Points.Add(new DataPoint(oxyplot_x, oxyplot_y));
                    model2.InvalidatePlot(true);
                }

                buffer_x = oxyplot_x;
                buffer_y = oxyplot_y;
            }



            
        }







        ///------------------------------------------ UI Values Update FUNCTION -------------------------------------------------------
        public async Task UI_textblock_update()
        {
            try
            {
                LineGraph content = Frame.Content as LineGraph;
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    //Raw Data
                    content.datax_box.Text = array3;
                    content.datay_box.Text = array4;
                    content.dataeuler_box.Text = angle_maze.ToString();

                    //Processing data
                    content.dataxzero_box.Text = zerox.ToString();
                    content.datayzero_box.Text = zeroy.ToString();
                    content.dataanglezero_box.Text = zeroangle.ToString();
                    content.dataxdisp_box.Text = x_disp.ToString();
                    content.dataydisp_box.Text = y_disp.ToString();
                    content.dataangleactual_box.Text = angle_actual_maze.ToString();

                    //Output Data
                    //content.absx_box.Text = oxyplot_x.ToString();
                    //content.absy_box.Text = oxyplot_y.ToString();

                    //Trial Timer
                    content.trial_timer_box.Text = trial_timer.Elapsed.Hours.ToString() + ":" + trial_timer.Elapsed.Minutes.ToString() + ":" + trial_timer.Elapsed.Seconds.ToString();

                    //Trial Status
                    if (trial_active == true)
                    {
                        content.trial_status_box.Text = "ACTIVE";
                        content.trial_status_box.Foreground = new SolidColorBrush(Colors.Green);
                    }

                    else
                    {
                        content.trial_status_box.Text = "INACTIVE";
                        content.trial_status_box.Foreground = new SolidColorBrush(Colors.Red); ;
                    }

                    //Oxy Plots
                    if (IMU_graph_en_state == true)
                    {
                        oxyplot_update_euler();
                    }

                    if (PIXY_plot_en_state == true && trial_active == true)
                    {
                        oxyplot_update_PIXY(oxyplot_within_boundaries);
                    }

                    //Program_output
                    if (func_IMU_init_success == true)
                    {
                        content.program_log.Text = "IMU Initialised successfully \r\n" + content.program_log.Text;
                        func_IMU_init_success = false;
                    }

                    if (func_IMU_euler_init_success == true)
                    {
                        content.program_log.Text = "IMU Euler pathway made successfully \r\n" + content.program_log.Text;
                        func_IMU_euler_init_success = false;
                    }

                    if (func_serial_init_success == true)
                    {
                        content.program_log.Text = "Serial Devices connected successfully \r\n" + content.program_log.Text;
                        func_serial_init_success = false;
                    }

                    if (func_pixy_read_success == true)
                    {
                        content.program_log.Text = "Serial PIXY data read successfully \r\n" + content.program_log.Text;
                        func_pixy_read_success = false;
                    }

                    if (func_folder_made_success == true)
                    {
                        content.program_log.Text = "Folder pathway made successfully \r\n" + content.program_log.Text;
                        func_folder_made_success = false;
                    }

                    if (func_plot_saved_success == true)
                    {
                        content.program_log.Text = "Oxyplot saved successfuly \r\n" + content.program_log.Text;
                        func_plot_saved_success = false;
                    }

                    if (func_x_outline_drawn_success == true)
                    {
                        content.program_log.Text = "X Maze outline drawn successfuly \r\n" + content.program_log.Text;
                        func_x_outline_drawn_success = false;
                    }

                    if (func_oxy_clear_success == true)
                    {
                        content.program_log.Text = "Oxyplot cleared successfuly \r\n" + content.program_log.Text;
                        func_oxy_clear_success = false;
                    }

                    if(toggle_zero_set_success ==  true)
                    {
                        content.program_log.Text = "Zero Point set successfuly \r\n" + content.program_log.Text;
                        toggle_zero_set_success = false;
                    }

                    if (toggle_data_rec_set_success == true)
                    {
                        content.program_log.Text = "Data Recording enabled successfuly \r\n" + content.program_log.Text;
                        toggle_data_rec_set_success = false;
                    }

                    if (toggle_plot_hist_set_success == true)
                    {
                        content.program_log.Text = "Oxyplot History enabled successfuly \r\n" + content.program_log.Text;
                        toggle_plot_hist_set_success = false;
                    }

                    if (toggle_IMU_plot_en_success == true)
                    {
                        content.program_log.Text = "IMU Plot enabled successfuly \r\n" + content.program_log.Text;
                        toggle_IMU_plot_en_success = false;
                    }

                    if (toggle_PIXY_plot_en_success == true)
                    {
                        content.program_log.Text = "Pixy Plot enabled successfuly \r\n" + content.program_log.Text;
                        toggle_PIXY_plot_en_success = false;
                    }

                    //Trial Log


                });
            }

            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("UI update error" + ex.Message);
            }
        }
























    }
}
