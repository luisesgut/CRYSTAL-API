using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;

namespace WebOs.Controllers
{
    public class ReporteController : ApiController
    {
        [HttpGet]
        [Route("api/generar-pdf")]
        public HttpResponseMessage GenerarPDF(string nombre = "Luis")
        {
            try
            {
                string rutaReporte = System.Web.Hosting.HostingEnvironment.MapPath("~/Reports/Test1.rpt");

                ReportDocument reporte = new ReportDocument();
                reporte.Load(rutaReporte);

                // Asignar el valor al parámetro
                reporte.SetParameterValue("nombre", nombre);

                // Exportar a PDF
                Stream pdf = reporte.ExportToStream(ExportFormatType.PortableDocFormat);
                pdf.Seek(0, SeekOrigin.Begin);

                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new StreamContent(pdf);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("inline")
                {
                    FileName = "reporte.pdf"
                };

                return response;
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, $"Error: {ex.Message}");
            }
        }
    }
}
