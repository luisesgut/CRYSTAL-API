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
    public class TestController : ApiController
    {
        [HttpGet]
        [Route("api/orden-pdf")]
        public HttpResponseMessage GenerarPorOT(string ot)
        {
            try
            {
                // Validar parámetro
                if (!int.TryParse(ot, out int otNumerico))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "❌ El parámetro 'OT' debe ser numérico.");

                // Verificar archivo
                string rutaReporte = System.Web.Hosting.HostingEnvironment.MapPath("~/Reports/TestV3.rpt");
                if (!File.Exists(rutaReporte))
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, $"❌ No se encontró el reporte en: {rutaReporte}");

                // Cargar reporte
                ReportDocument reporte = new ReportDocument();
                reporte.Load(rutaReporte);
                reporte.Refresh();

                // Conexión a la BD
                ConnectionInfo conn = new ConnectionInfo
                {
                    ServerName = "172.16.10.113,1433",
                    DatabaseName = "bioflex_sap",
                    UserID = "User_PBI",
                    Password = "PBI*2025",
                    IntegratedSecurity = false
                };

                foreach (Table table in reporte.Database.Tables)
                {
                    TableLogOnInfo logonInfo = table.LogOnInfo;
                    logonInfo.ConnectionInfo = conn;
                    table.ApplyLogOnInfo(logonInfo);
                    table.Location = $"{conn.DatabaseName}.dbo.{table.Name}";
                }

                // Parámetro OT
                reporte.SetParameterValue("OT", otNumerico);

                // Exportar
                Stream pdf = reporte.ExportToStream(ExportFormatType.PortableDocFormat);
                pdf.Seek(0, SeekOrigin.Begin);

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StreamContent(pdf)
                };
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("inline")
                {
                    FileName = $"Orden_{otNumerico}.pdf"
                };

                return response;
            }
            catch (Exception ex)
            {
                // Analiza el mensaje para dar una mejor respuesta
                string msg = ex.Message.ToLower();

                if (msg.Contains("logon failed") || msg.Contains("no se pudo conectar"))
                {
                    return Request.CreateErrorResponse(HttpStatusCode.Unauthorized,
                        "❌ No se pudo conectar con la base de datos. Verifica usuario, clave o permisos.");
                }

                if (msg.Contains("missing parameter values") || msg.Contains("parámetro"))
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                        "❌ Faltan parámetros requeridos en el reporte.");
                }

                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError,
                    $"❌ Error inesperado al generar el reporte.\n➡ {ex.Message}");
            }
        }
    }
}
