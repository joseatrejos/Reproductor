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
        private ISampleProvider fuente;

        // Declaración de Variables
        private int tamañoBuffer;
        private int duracionBufferSegundos;
        private int cantidadMuestrasTranscurridas = 0;
        private int cantidadMuestrasBorradas = 0;
        private int cantidadMuestrasOffset = 0;
        
        public int OffsetMilisegundos { get; set; }

        private List<float> bufferDelay = new List<float>();

        // El WaveFormat conlleva: Bit Depth, Sample Rate y Channels
        public WaveFormat WaveFormat
        {
            get
            {
                return fuente.WaveFormat;
            }
        }

        public Delay(ISampleProvider fuente)
        {
            this.fuente = fuente;
            OffsetMilisegundos = 500;
            duracionBufferSegundos = 10;
            cantidadMuestrasOffset = (int)((float)OffsetMilisegundos / 1000);

            // Buffer   =         Muestras por segundo * Tiempo en segundos
            tamañoBuffer = fuente.WaveFormat.SampleRate * duracionBufferSegundos;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            var read = fuente.Read(buffer, offset, count);

            float tiempoTranscurridoSegundos = (float)cantidadMuestrasTranscurridas / (float)fuente.WaveFormat.SampleRate;

            for(int i = 0; i < read; i++)
            {
                bufferDelay.Add(buffer[i + offset]);
            }

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
            }

            // Aplicar el efecto
            if(tiempoTranscurridoSegundos > OffsetMilisegundos)
            {
                for(int i = 0; i < read; i ++)
                {

                    buffer[offset + i] += bufferDelay[cantidadMuestrasTranscurridas - cantidadMuestrasBorradas + i - cantidadMuestrasOffset];
                }
            }

            cantidadMuestrasTranscurridas += read;
            return read;
        }
    }
}