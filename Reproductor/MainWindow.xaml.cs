using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Windows.Threading;
using Microsoft.Win32;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Reproductor
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        AudioFileReader reader;

        // Nuestra comunicación con la tarjeta de sonido
        WaveOutEvent output;

        DispatcherTimer timer;
        VolumeSampleProvider volume;
        FadeInOutSampleProvider fades;
        bool fadingOut = false;
        
        bool dragging = false;

        public MainWindow()
        {
            InitializeComponent();
            LlenarComboSalida();

            // Inicializar el timer
            timer = new DispatcherTimer();

            // Definir el intervalo durante el cual se ejecutará cada hilo
            timer.Interval = TimeSpan.FromMilliseconds(1000);

            // Establecer el proceso que se ejecutará
            timer.Tick += Timer_Tick;

            
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (reader != null)
            {
                lbl_Tiempo_Actual.Text = reader.CurrentTime.ToString().Substring(0, 8);
                if (!dragging)
                {
                    sld_Reproduccion.Value = reader.CurrentTime.TotalSeconds;
                }
            }
        }

        private void LlenarComboSalida()
        {
            cb_Salida.Items.Clear();
            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                WaveOutCapabilities capacidades = WaveOut.GetCapabilities(i);
                cb_Salida.Items.Add(capacidades.ProductName);
            }
            cb_Salida.SelectedIndex = 0;
        }

        private void btn_Elegir_Archivo_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if(openFileDialog.ShowDialog() == true)
            {
                txt_Direccion_Archivo.Text = openFileDialog.FileName;
            }
        }

        private void btn_Reproducir_Click(object sender, RoutedEventArgs e)
        {
            if (output != null && output.PlaybackState == PlaybackState.Paused)
            {
                output.Play();
                btn_Reproducir.IsEnabled = false;
                btn_Pausa.IsEnabled = true;
                btn_Detener.IsEnabled = true;
            }
            else
            {
                if (txt_Direccion_Archivo.Text != "") { 
                    reader = new AudioFileReader(txt_Direccion_Archivo.Text);

                    fades = new FadeInOutSampleProvider(reader, true);
                    double milisegundosFadeIn = Double.Parse(txt_FadeIn.Text)*1000.0;
                    fades.BeginFadeIn(milisegundosFadeIn);
                    fadingOut = false;

                    output = new WaveOutEvent();

                    output.DeviceNumber = cb_Salida.SelectedIndex;

                    // Los rayitos(eventos) responden a funciones mediante el operador +=
                    output.PlaybackStopped += Output_PlaybackStopped;

                    volume = new VolumeSampleProvider(reader);

                    volume.Volume = (float)sld_Volumen.Value;
                    
                    output.Init(volume);
                    output.Play();

                    btn_Pausa.IsEnabled = true;
                    btn_Detener.IsEnabled = true;
                    btn_Reproducir.IsEnabled = false;

                    lbl_Tiempo_Total.Text = reader.TotalTime.ToString().Substring(0, 8);
                    lbl_Tiempo_Actual.Text = reader.CurrentTime.ToString().Substring(0, 8);

                    sld_Reproduccion.Maximum = reader.TotalTime.TotalSeconds;
                    sld_Reproduccion.Minimum = 0;

                    timer.Start();
                }
            }
        }

        private void Output_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            reader.Dispose();
            output.Dispose();
            timer.Stop();
        }

        private void btn_Pausa_Click(object sender, RoutedEventArgs e)
        {
            if (output != null)
            {
                output.Pause();

                btn_Reproducir.IsEnabled = true;
                btn_Pausa.IsEnabled = false;
                btn_Detener.IsEnabled = true;
            }

        }

        private void btn_Detener_Click(object sender, RoutedEventArgs e)
        {
            output.Stop();

            btn_Reproducir.IsEnabled = true;
            btn_Pausa.IsEnabled = false;
            btn_Detener.IsEnabled = false;
            txt_Direccion_Archivo.Text = "";
            sld_Reproduccion.Value = 0;
            lbl_Tiempo_Actual.Text = "00:00";
            lbl_Tiempo_Total.Text = "00:00";
        }
        
        private void sld_Reproduccion_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            dragging = true;
        }

        private void sld_Reproduccion_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            dragging = false;
            if( reader != null && output != null && (output.PlaybackState != PlaybackState.Stopped) )
            {
                reader.CurrentTime = TimeSpan.FromSeconds(sld_Reproduccion.Value);
            }
        }

        private void sld_Volumen_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (volume != null & output != null && output.PlaybackState != PlaybackState.Stopped)
            {
                volume.Volume = (float)sld_Volumen.Value;
            }
            if (lbl_Volumen_Cantidad != null)
            {
                lbl_Volumen_Cantidad.Text = ((int)(sld_Volumen.Value * 100)).ToString() + "%";
            }
        }

        private void btn_FadeOut_Click(object sender, RoutedEventArgs e)
        {
            if(!fadingOut && fades!= null && output != null)
            {
                fadingOut = true;
                double milisegundosFadeOut = Double.Parse(txt_FadeOut.Text) * 1000.0;
                fades.BeginFadeOut(milisegundosFadeOut);
            }
        }
    }
}