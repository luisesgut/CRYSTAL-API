using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace WebOs.Controllers.UpdateRPTS
{
    [RoutePrefix("api/rpt")]
    public class UpdateRPTController : ApiController
    {
        private readonly string _basePath = @"C:\Users\DESARROLLOS\Documents\CrystalReports\ReportesRPT";
        private readonly string _backupPath = @"\\LEX\Users\DESARROLLOS\Documents\CrystalReports\Respaldos";

        // GET: api/rpt/listar?folder=Extrusion/ConsumoOT
        [HttpGet]
        [Route("listar")]
        public IHttpActionResult ListarReportes(string folder)
        {
            var targetFolder = Path.Combine(_basePath, folder);
            if (!Directory.Exists(targetFolder))
                return NotFound();

            var archivos = Directory.GetFiles(targetFolder, "*.rpt")
                .Select(f => new
                {
                    Nombre = Path.GetFileName(f),
                    Modificado = File.GetLastWriteTime(f)
                });

            return Ok(archivos);
        }

        // POST: api/rpt/subir
        [HttpPost]
        [Route("subir")]
        public async Task<IHttpActionResult> SubirReporte()
        {
            var provider = new MultipartMemoryStreamProvider();
            await Request.Content.ReadAsMultipartAsync(provider);

            var file = provider.Contents.FirstOrDefault();
            if (file == null)
                return BadRequest("No se recibió ningún archivo.");

            string folder = HttpContext.Current.Request.Form["folder"];
            string fileName = HttpContext.Current.Request.Form["fileName"];

            if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(fileName))
                return BadRequest("Faltan parámetros 'folder' y/o 'fileName'.");

            if (!fileName.EndsWith(".rpt", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Solo se permiten archivos con extensión .rpt");

            string destinoCarpeta = Path.Combine(_basePath, folder);
            Directory.CreateDirectory(destinoCarpeta);
            Directory.CreateDirectory(_backupPath);

            string rutaFinal = Path.Combine(destinoCarpeta, fileName);
            string rutaBackup = Path.Combine(_backupPath, $"{Path.GetFileNameWithoutExtension(fileName)}_{DateTime.Now:yyyyMMdd_HHmmss}.bak.rpt");

            // Respaldar si existe
            if (File.Exists(rutaFinal))
                File.Copy(rutaFinal, rutaBackup);

            var contenido = await file.ReadAsByteArrayAsync();
            File.WriteAllBytes(rutaFinal, contenido);

            return Ok("✅ Archivo actualizado correctamente.");
        }

        // DELETE: api/rpt/eliminar?folder=Extrusion/ConsumoOT&fileName=Reporte1.rpt
        [HttpDelete]
        [Route("eliminar")]
        public IHttpActionResult EliminarReporte(string folder, string fileName)
        {
            string ruta = Path.Combine(_basePath, folder, fileName);
            if (!System.IO.File.Exists(ruta))
                return NotFound();

            File.Delete(ruta);
            return Ok("✅ Archivo eliminado correctamente.");
        }

        // GET: api/rpt/descargar?folder=Extrusion/ConsumoOT&fileName=Reporte1.rpt
        [HttpGet]
        [Route("descargar")]
        public HttpResponseMessage DescargarReporte(string folder, string fileName)
        {
            string path = Path.Combine(_basePath, folder, fileName);
            if (!File.Exists(path))
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Archivo no encontrado.");

            var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(stream)
            };
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
            {
                FileName = fileName
            };

            return response;
        }
    }
}
