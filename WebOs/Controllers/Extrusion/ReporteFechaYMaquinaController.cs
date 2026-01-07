using CrystalDecisions.CrystalReports.Engine;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using WebOs.Services;
using WebOs.Services.Extrusion;

namespace WebOs.Controllers
{
    public class ReporteFechaYMaquinaController : ApiController
    {
        private readonly CrystalReportServiceExtrusion _reportService = new CrystalReportServiceExtrusion();
        private readonly ArchivoService _archivoService = new ArchivoService();
        private readonly string _storagePath = @"\\LEX\Users\DESARROLLOS\Documents\CrystalReports\Extrusion\ReporteFechaMaquina";

        [HttpGet]
        [Route("api/fechaYmaquina")]
        public HttpResponseMessage GenerarPorFechaYMaquina(string fecha, string maquina, string turno = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fecha) || string.IsNullOrWhiteSpace(maquina))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "❌ Parámetros 'fecha' y 'maquina' obligatorios.");

                string rutaReporte = @"C:\Users\DESARROLLOS\Documents\CrystalReports\ReportesRPT\Extrusion\ReporteFechaMaquinaYTurnoV4.rpt";

                // ✅ Cargar con credenciales (incluye subreportes) usando el servicio Extrusion
                ReportDocument reporte = _reportService.CargarReporte(rutaReporte);

                // Params
                reporte.SetParameterValue("fecha", fecha);
                reporte.SetParameterValue("maquina", maquina);
                reporte.SetParameterValue("turno", turno ?? "");

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                // Ojo: el nombre trae fecha/maquina, puede traer caracteres raros (/, :, etc.)
                // Si tu 'fecha' viene como "2026-01-06" no hay tema.
                string fileName = $"Reporte_{fecha}_{maquina}_{timestamp}.pdf";

                _archivoService.AsegurarCarpeta(_storagePath);

                // ✅ Exportar usando el método del servicio (cierra y libera reporte)
                _reportService.ExportarPDF(reporte, _storagePath, fileName);

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent($"✅ Reporte generado exitosamente. Archivo: {fileName}")
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

        [HttpGet]
        [Route("api/fechaYmaquina/vistaPrevia")]
        public HttpResponseMessage VistaPrevia(string fileName)
        {
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

        [HttpGet]
        [Route("api/fechaYmaquina/recientes")]
        public IHttpActionResult ArchivosRecientes()
        {
            var archivos = _archivoService.ObtenerArchivosRecientes(_storagePath, 5);
            return Ok(new { archivos });
        }

        [HttpGet]
        [Route("api/fechaYmaquina/descargar")]
        public HttpResponseMessage DescargarArchivo(string fileName)
        {
            try
            {
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
