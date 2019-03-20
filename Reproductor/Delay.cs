using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace Reproductor
{
    class Delay : ISampleProvider
    {
        // Declaración de Variables
        private int tamañoBuffer;
        private int duracionBufferSegundos;
        private int cantidadMuestrasTranscurridas = 0;
        private int cantidadMuestrasBorradas = 0;
        private int cantidadMuestrasOffset = 0;

        public bool Activo
        {
            get;set;
        }

        private int offsetMilisegundos;
        public int OffsetMilisegundos
        {
            get
            {
                return offsetMilisegundos;
            }
            set
            {
                offsetMilisegundos = value;
                cantidadMuestrasOffset = (int)(((float)OffsetMilisegundos / 1000.0f) * (float)fuente.WaveFormat.SampleRate);
            }
        }

        private ISampleProvider fuente;

        private List<float> bufferDelay = new List<float>();
        
        public Delay(ISampleProvider fuente)
        {
            Activo = false;
            this.fuente = fuente;
            duracionBufferSegundos = 10;

            // Buffer   =         Muestras por segundo * Tiempo en segundos
            tamañoBuffer = fuente.WaveFormat.SampleRate * duracionBufferSegundos; ;
        }

        // El WaveFormat conlleva: Bit Depth, Sample Rate y Channels
        public WaveFormat WaveFormat
        {
            get
            {
                return fuente.WaveFormat;
            }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            // Se leen las muestras de la señal fuente
            var read = fuente.Read(buffer, offset, count);

            // Se calcula el tiempo transcurrido
            float tiempoTranscurridoSegundos = (float)cantidadMuestrasTranscurridas / (float)fuente.WaveFormat.SampleRate;
            float milisegundosTranscurridos = tiempoTranscurridoSegundos * 1000.0f; // Segundos a miilisegundos

            // Llenado del buffer
            for (int i = 0; i < read; i++)
            {
                bufferDelay.Add(buffer[i + offset]);
            }

            // Eliminado de excedentes del buffer
            if (bufferDelay.Count > tamañoBuffer)
            {
                int diferencia = bufferDelay.Count - tamañoBuffer;
                bufferDelay.RemoveRange(0, diferencia);
                cantidadMuestrasBorradas += diferencia;
            }

            // Eliminar los excedentes del buffer
            if(bufferDelay.Count > tamañoBuffer)
            {
                int diferencia = bufferDelay.Count - tamañoBuffer;
                bufferDelay.RemoveRange(0, diferencia);
                cantidadMuestrasBorradas += diferencia;
            }

            if (Activo == true)
            {
                // Aplicación del efecto
                if (milisegundosTranscurridos > offsetMilisegundos)
                {
                    for (int i = 0; i < read; i++)
                    {
                        buffer[offset + i] += bufferDelay[cantidadMuestrasTranscurridas - cantidadMuestrasBorradas + i - cantidadMuestrasOffset];
                    }
                }
            }

            cantidadMuestrasTranscurridas += read;
            return read;
        }
    }
}