using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace Reproductor
{
    class EfectoVolumen : ISampleProvider
    {
        private ISampleProvider fuente;

        public EfectoVolumen(ISampleProvider fuente)
        {
            // El de arriba = el parámetro
            this.fuente = fuente;
        }

        // El WaveFormat conlleva: Bit Depth, Sample Rate y Channels
        public WaveFormat WaveFormat {
            get
            {
                return fuente.WaveFormat;
            }
        }

        /*  Los valores del búffer irán directamente a la salida, el offset es para aplicar desfaces 
            y count representa el número de muestras */
        public int Read(float[] buffer, int offset, int count)
        {
            var read = fuente.Read(buffer, offset, count);
            
            // Recorremos las muestras leídas para aplicar el efecto deseado
            for (int i = 0; i < read; i++)
            {
                // En caso de ocupar un offset, se considera sumandoselo a la variable 'i'
                buffer[offset + i] *= 0.2f;
            }

            return read;
        }
    }
}
