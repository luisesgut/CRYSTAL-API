using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using CrystalDecisions.CrystalReports.Engine;
using WebOs.Services;
using WebOs.Services.Extrusion;

namespace WebOs.Controllers.Impresion
{
    public class ResumenImpresionController : ApiController
    {
        private readonly CrystalReportService _reportService = new CrystalReportService();
        private readonly ArchivoService _archivoService = new ArchivoService();
        private readonly string _storagePath = @"\\LEX\Users\DESARROLLOS\Documents\CrystalReports\Impresion\Resumen";

        [HttpGet]
        [Route("api/ResumenImpresion")]
        public HttpResponseMessage GenerarResumen(int año, int mes)
        {
            try
            {
                if (año <= 0 || mes < 1 || mes > 12)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "❌ Parámetros inválidos. Año y mes deben ser válidos.");

                string rutaReporte = System.Web.Hosting.HostingEnvironment.MapPath("~/Reports/ReportsImpresion/ReporteResumenImp.rpt");

                ReportDocument reporte = _reportService.CargarReporte(rutaReporte);
                reporte.SetParameterValue("AÑO", año);
                reporte.SetParameterValue("MES", mes);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"ResumenImpresion_{año}_{mes:D2}_{timestamp}.pdf";

                _archivoService.AsegurarCarpeta(_storagePath);
                string rutaDestino = Path.Combine(_storagePath, fileName);

                reporte.ExportToDisk(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat, rutaDestino);
                reporte.Close();
                reporte.Dispose();

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent($"✅ Reporte generado exitosamente. Archivo: {fileName}")
                };
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, $"❌ Error inesperado al generar el reporte.\n➡ {ex.Message}");
            }
        }

        [HttpGet]
        [Route("api/ResumenImpresion/vistaPrevia")]
        public HttpResponseMessage VistaPrevia(string fileName)
        {
            string path = Path.Combine(_storagePath, fileName);
            if (!File.Exists(path))
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Archivo no encontrado.");

            var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
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
        [Route("api/ResumenImpresion/recientes")]
        public IHttpActionResult ArchivosRecientes()
        {
            var archivos = _archivoService.ObtenerArchivosRecientes(_storagePath, 5);
            return Ok(new { archivos });
        }

        [HttpGet]
        [Route("api/ResumenImpresion/descargar")]
        public HttpResponseMessage DescargarArchivo(string fileName)
        {
            try
            {
                string filePath = Path.Combine(_storagePath, fileName);

                if (!System.IO.File.Exists(filePath))
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
