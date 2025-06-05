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
    public class ImpresionReportePorOTController : ApiController
    {
        private readonly CrystalReportService _reportService = new CrystalReportService();
        private readonly ArchivoService _archivoService = new ArchivoService();
        private readonly string _storagePath = @"\\LEX\Users\DESARROLLOS\Documents\CrystalReports\Impresion\ReporteOT";

        [HttpGet]
        [Route("api/ImpresionReportePorOT")]
        public HttpResponseMessage GenerarPorOT(string ot)
        {
            try
            {
                if (!int.TryParse(ot, out int otNumerico))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "❌ El parámetro 'OT' debe ser numérico.");

                string rutaReporte = System.Web.Hosting.HostingEnvironment.MapPath("~/Reports/ReportsImpresion/ReporteImpOT.rpt");

                ReportDocument reporte = _reportService.CargarReporte(rutaReporte);
                reporte.SetParameterValue("OT", otNumerico);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"ReporteOT_{otNumerico}_{timestamp}.pdf";

                _archivoService.AsegurarCarpeta(_storagePath);
                string rutaDestino = Path.Combine(_storagePath, fileName);

                reporte.ExportToDisk(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat, rutaDestino);
                reporte.Close();
                reporte.Dispose();

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent($"✅ Reporte generado exitosamente. Archivo: {fileName}")
                };
                return response;
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, $"❌ Error inesperado al generar el reporte.\n➡ {ex.Message}");
            }
        }

        //vista previa
        [HttpGet]
        [Route("api/ImpresionReportePorOT/vistaPrevia")]
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
        //listar archivos
        [HttpGet]
        [Route("api/ImpresionReportePorOT/recientes")]
        public IHttpActionResult ArchivosRecientes()
        {
            var archivoService = new ArchivoService();
            var archivos = archivoService.ObtenerArchivosRecientes(_storagePath, 5);

            return Ok(new { archivos }); // Retorna como: { "archivos": [ "archivo1.pdf", "archivo2.pdf", ... ] }
        }

        //descargar archivo
        [HttpGet]
        [Route("api/ImpresionReportePorOT/descargar")]
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
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") // 👈 importante para forzar la descarga
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
