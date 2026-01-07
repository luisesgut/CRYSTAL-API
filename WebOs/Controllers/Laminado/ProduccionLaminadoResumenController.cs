using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using WebOs.Services.Laminado; 

namespace WebOs.Controllers.Laminado
{
    
    [RoutePrefix("api/laminado/produccion")]
    public class ProduccionLaminadoResumenController : ApiController
    {
        private readonly CrystalReportServiceLaminado _reportService = new CrystalReportServiceLaminado();

        // Carpeta destino donde se guardarán los PDFs generados
        private readonly string _storagePath = @"C:\Users\DESARROLLOS\Documents\CrystalReports\Laminado\LaminadoResumen";
        private readonly string _rutaReporte = @"C:\Users\DESARROLLOS\Documents\CrystalReports\ReportesRPT\Laminado\ProduccionLaminadoResumen.rpt";

        // Nombres EXACTOS de parámetros en el .RPT
        private const string PARAM_FECHA = "Fecha";
       

        [HttpGet]
        [Route("generar")]
        public HttpResponseMessage Generar(string fecha)
        {
            if (string.IsNullOrWhiteSpace(fecha))
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "❌ El parámetro 'fecha' es requerido.");

            ReportDocument reporte = null;
            try
            {
                // Cargar RPT con credenciales ya aplicadas desde el servicio
                reporte = _reportService.CargarReporte(_rutaReporte);

                // Pasar parámetros (string)
                reporte.SetParameterValue(PARAM_FECHA, fecha);
               

                // Asegurar carpeta destino
                AsegurarCarpeta(_storagePath);

                // Construir nombre archivo
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string safeFecha = SanitizeFileName(fecha);
                string fileName = $"Reporte_{safeFecha}_{timestamp}.pdf";
                string rutaDestino = Path.Combine(_storagePath, fileName);

                // Exportar a PDF
                reporte.ExportToDisk(ExportFormatType.PortableDocFormat, rutaDestino);

                // Opcional: si prefieres usar el método del servicio para cerrar/limpiar:
                // _reportService.ExportarPDF(reporte, _storagePath, fileName);

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent($"✅ Reporte generado exitosamente.\nArchivo: {fileName}")
                };
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(
                    HttpStatusCode.InternalServerError,
                    $"❌ Error al generar el reporte de Laminado.\n➡ {ex.Message}"
                );
            }
            finally
            {
                if (reporte != null)
                {
                    try { reporte.Close(); } catch { /* ignore */ }
                    try { reporte.Dispose(); } catch { /* ignore */ }
                }
            }
        }

        // ========== 2) VISTA PREVIA INLINE (Content-Disposition: inline) ==========
        [HttpGet]
        [Route("vistaPrevia")]
        public HttpResponseMessage VistaPrevia(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "El parámetro 'fileName' es requerido.");

            string path = Path.Combine(_storagePath, fileName);
            if (!File.Exists(path))
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Archivo no encontrado.");

            var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(fs)
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("inline")
            {
                FileName = fileName
            };

            return response;
        }

        // ========== 3) LISTAR RECIENTES ==========
        [HttpGet]
        [Route("recientes")]
        public IHttpActionResult ArchivosRecientes(int top = 10)
        {
            var archivos = ObtenerArchivosRecientes(_storagePath, top);
            return Ok(new { archivos });
        }

        // ========== 4) DESCARGAR ==========
        [HttpGet]
        [Route("descargar")]
        public HttpResponseMessage Descargar(string fileName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "El parámetro 'fileName' es requerido.");

                string filePath = Path.Combine(_storagePath, fileName);
                if (!File.Exists(filePath))
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "❌ El archivo no se encuentra en el servidor.");

                byte[] fileBytes = File.ReadAllBytes(filePath);

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(fileBytes)
                };
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = fileName
                };

                return response;
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, $"❌ Error al descargar el archivo.\n➡ {ex.Message}");
            }
        }

        [HttpGet]
        [Route("ping")]
        public IHttpActionResult Ping() => Ok("pong");


        // ================= Helpers locales =================
        private static void AsegurarCarpeta(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        private static IEnumerable<object> ObtenerArchivosRecientes(string path, int top)
        {
            if (!Directory.Exists(path)) return Enumerable.Empty<object>();

            return new DirectoryInfo(path)
                .EnumerateFiles("*.pdf", SearchOption.TopDirectoryOnly)
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .Take(top)
                .Select(f => new
                {
                    f.Name,
                    f.FullName,
                    f.Length,
                    LastWriteTime = f.LastWriteTime,
                    LastWriteTimeUtc = f.LastWriteTimeUtc
                })
                .ToList();
        }

        private static string SanitizeFileName(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            foreach (char c in Path.GetInvalidFileNameChars())
                input = input.Replace(c.ToString(), string.Empty);
            return input.Replace(" ", "_");
        }
    }
}
