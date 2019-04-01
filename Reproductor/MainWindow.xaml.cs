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
    public partial class MainWindow : Window
    {
        AudioFileReader reader;

        // Nuestra comunicación con la tarjeta de sonido
        WaveOutEvent output;

        DispatcherTimer timer;
        EfectoVolumen volume;
        FadeInOutSampleProvider fades;
        Delay delay;

        bool fadingOut = false;
        bool dragging = false;
        int estado = 0;

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

        // Time Functions
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

        // Inicializacón
        private void LlenarComboSalida()
        {
            cb_Salida.Items.Clear();
            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                WaveOutCapabilities capacidades = WaveOut.GetCapabilities(i);
                cb_Salida.Items.Add(capacidades.ProductName);
            }
            cb_Salida.SelectedIndex = 0;
            sld_Reproduccion.IsEnabled = false;
        }

        // Botón Elegir Archivo
        private void btn_Elegir_Archivo_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if(openFileDialog.ShowDialog() == true)
            {
                txt_Direccion_Archivo.Text = openFileDialog.FileName;
            }
        }

        // Boton Reproducir
        private void btn_Reproducir_Click(object sender, RoutedEventArgs e)
        {
            if (output != null && output.PlaybackState == PlaybackState.Paused)
            {
                sld_Reproduccion.IsEnabled = true;
                output.Play();
                btn_Reproducir.IsEnabled = false;
                btn_Elegir_Archivo.IsEnabled = false;
                btn_Pausa.IsEnabled = true;
                btn_Detener.IsEnabled = true;
            }
            else
            {
                if (txt_Direccion_Archivo.Text != "") { 
                    reader = new AudioFileReader(txt_Direccion_Archivo.Text);
                    
                    delay = new Delay(reader);

                    delay.Ganancia = (float)sld_Gain_Cantidad.Value;

                    delay.OffsetMilisegundos = (int)sld_Delay_Offset.Value;

                    if(sld_Delay_Offset.IsEnabled == true)
                        delay.Activo = true;
                    else
                        delay.Activo = false;

                    fades = new FadeInOutSampleProvider(delay, true);
                    double milisegundosFadeIn = Double.Parse(txt_FadeIn.Text)*1000.0;
                    fades.BeginFadeIn(milisegundosFadeIn);
                    fadingOut = false;

                    output = new WaveOutEvent();

                    // Mientras más alta sea la latencia, más info se podrá almacenar en el búffer, a costa de que éste tardará un poco más (la 1ra vez) en llenarse 
                    output.DesiredLatency = 150; // 150 ms

                    output.DeviceNumber = cb_Salida.SelectedIndex;

                    // Los rayitos(eventos) responden a funciones mediante el operador +=
                    output.PlaybackStopped += Output_PlaybackStopped;

                    volume = new EfectoVolumen(fades);

                    volume.Volume = (float)sld_Volumen.Value;
                    
                    output.Init(volume);
                    output.Play();

                    sld_Reproduccion.IsEnabled = true;
                    btn_Pausa.IsEnabled = true;
                    btn_Detener.IsEnabled = true;
                    btn_Reproducir.IsEnabled = false;
                    btn_Elegir_Archivo.IsEnabled = false;

                    lbl_Tiempo_Total.Text = reader.TotalTime.ToString().Substring(0, 8);
                    lbl_Tiempo_Actual.Text = reader.CurrentTime.ToString().Substring(0, 8);

                    sld_Reproduccion.Maximum = reader.TotalTime.TotalSeconds;
                    sld_Reproduccion.Minimum = 0;

                    timer.Start();
                }
            }
        }

        // Botón Pausa
        private void btn_Pausa_Click(object sender, RoutedEventArgs e)
        {
            if (output != null)
            {
                output.Pause();

                btn_Reproducir.IsEnabled = true;
                btn_Pausa.IsEnabled = false;
                btn_Detener.IsEnabled = true;
                sld_Reproduccion.IsEnabled = true;
            }
        }

        // Botón Detener
        private void btn_Detener_Click(object sender, RoutedEventArgs e)
        {
            output.Stop();

            sld_Reproduccion.IsEnabled = false;
            btn_Reproducir.IsEnabled = true;
            btn_Elegir_Archivo.IsEnabled = true;
            btn_Pausa.IsEnabled = false;
            btn_Detener.IsEnabled = false;
            txt_Direccion_Archivo.Text = "";
            sld_Reproduccion.Value = 0;
            lbl_Tiempo_Actual.Text = "00:00";
            lbl_Tiempo_Total.Text = "00:00";
        }

        // Time Function al Detener
        private void Output_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            reader.Dispose();
            output.Dispose();
            timer.Stop();
        }

        // Funciones de Drag del Slider de la Música
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

        // Slider del Volumen (Applies Changes to the label)
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

        // Botón FadeOut
        private void btn_FadeOut_Click(object sender, RoutedEventArgs e)
        {
            if(!fadingOut && fades!= null && output != null)
            {
                fadingOut = true;
                double milisegundosFadeOut = Double.Parse(txt_FadeOut.Text) * 1000.0;
                fades.BeginFadeOut(milisegundosFadeOut);
            }
        }

        // CheckBox Delay (Enables/Disables the Slider)
        private void ckb_Delay_Clicked(object sender, RoutedEventArgs e)
        {
            if (delay!=null)
                delay.Activo = (bool)ckb_Delay.IsChecked;

            sld_Delay_Offset.IsEnabled = (bool)ckb_Delay.IsChecked;
        }
        // Slider Delay (Applies Offset Changes to the label)
        private void sld_Delay_Offset_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if ((bool)ckb_Delay.IsChecked == true)
            {
                lbl_Delay_Offset.Text = ((int)(sld_Delay_Offset.Value)).ToString() + " ms";
            }

            if(delay != null)
            {
                delay.OffsetMilisegundos = (int)sld_Delay_Offset.Value;
            }
        }

        // Slider Gain (Applies Changes to the label)
        private void sld_Gain_Cantidad_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(lbl_Delay != null)
                lbl_Gain_Cantidad.Text = ((float)(sld_Gain_Cantidad.Value)).ToString("F");

            if (delay != null)
                delay.Ganancia = (float)sld_Gain_Cantidad.Value;
        }
    }
}