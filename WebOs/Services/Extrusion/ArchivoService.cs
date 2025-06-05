using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WebOs.Services.Extrusion
{
    public class ArchivoService
    {
        public void AsegurarCarpeta(string ruta)
        {
            if (!Directory.Exists(ruta))
                Directory.CreateDirectory(ruta);
        }

        public List<string> ObtenerArchivosRecientes(string ruta, int cantidad = 5)
        {
            if (!Directory.Exists(ruta)) return new List<string>();

            return Directory.GetFiles(ruta, "*.pdf")
                            .OrderByDescending(f => File.GetCreationTime(f))
                            .Take(cantidad)
                            .Select(Path.GetFileName)
                            .ToList();
        }

        public byte[] LeerArchivo(string rutaCompleta)
        {
            return File.Exists(rutaCompleta) ? File.ReadAllBytes(rutaCompleta) : null;
        }
    }
}
