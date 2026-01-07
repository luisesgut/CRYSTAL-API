using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using CrystalDecisions.CrystalReports.Engine;
using WebOs.Services;
using WebOs.Services.Extrusion;

namespace WebOs.Controllers.Extrusion
{
    public class ReportePorOTController : ApiController
    {
        private readonly CrystalReportServiceExtrusion _reportService = new CrystalReportServiceExtrusion();
        private readonly ArchivoService _archivoService = new ArchivoService();
        private readonly string _storagePath = @"\\LEX\Users\DESARROLLOS\Documents\CrystalReports\Extrusion\ReporteOT";

        [HttpGet]
        [Route("api/reportePorOT")]
        public HttpResponseMessage GenerarPorOT(string ot)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ot))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "❌ El parámetro 'OT' es obligatorio.");

                if (!int.TryParse(ot, out int otNumerico))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "❌ El parámetro 'OT' debe ser numérico.");

                string rutaReporte = @"C:\Users\DESARROLLOS\Documents\CrystalReports\ReportesRPT\Extrusion\ReportePorOTV2.rpt";

                // ✅ Carga con credenciales + subreportes (tu servicio robusto)
                ReportDocument reporte = _reportService.CargarReporte(rutaReporte);

                // ✅ Setear parámetro
                reporte.SetParameterValue("OT", otNumerico);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"ReporteOT_{otNumerico}_{timestamp}.pdf";

                _archivoService.AsegurarCarpeta(_storagePath);

                // ✅ Exportar y liberar recursos (tu ExportarPDF ya hace Close/Dispose)
                _reportService.ExportarPDF(reporte, _storagePath, fileName);

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent($"✅ Reporte generado exitosamente. Archivo: {fileName}", System.Text.Encoding.UTF8)
                };
            }
            catch (FileNotFoundException ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, $"❌ No se encontró el reporte RPT.\n➡ {ex.Message}");
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, $"❌ Error inesperado al generar el reporte.\n➡ {ex.Message}");
            }
        }

        // Vista previa (inline)
        [HttpGet]
        [Route("api/reportePorOT/vistaPrevia")]
        public HttpResponseMessage VistaPrevia(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "❌ fileName es obligatorio.");

            // ✅ Evitar path traversal
            fileName = Path.GetFileName(fileName);

            string path = Path.Combine(_storagePath, fileName);
            if (!File.Exists(path))
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Archivo no encontrado.");

            var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(fileStream)
            };

            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("inline")
            {
                FileName = fileName
            };

            return response;
        }

        // Listar archivos recientes
        [HttpGet]
        [Route("api/reportePorOT/recientes")]
        public IHttpActionResult ArchivosRecientes()
        {
            var archivos = _archivoService.ObtenerArchivosRecientes(_storagePath, 5);
            return Ok(new { archivos });
        }

        // Descargar archivo (attachment)
        [HttpGet]
        [Route("api/reportePorOT/descargar")]
        public HttpResponseMessage DescargarArchivo(string fileName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "❌ fileName es obligatorio.");

                // ✅ Evitar path traversal
                fileName = Path.GetFileName(fileName);

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
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, $"❌ Error al intentar descargar el archivo.\n➡ {ex.Message}");
            }
        }
    }
}
