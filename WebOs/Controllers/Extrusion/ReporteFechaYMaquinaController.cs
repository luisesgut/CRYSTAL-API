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

namespace WebOs.Controllers
{
    public class ReporteFechaYMaquinaController : ApiController
    {
        [HttpGet]
        [Route("api/fechaYmaquina")]
        public HttpResponseMessage GenerarPorFechaYMaquina(string fecha, string maquina)
        {
            try
            {
                // Validar parámetros
                if (string.IsNullOrEmpty(fecha) || string.IsNullOrEmpty(maquina))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "❌ Los parámetros 'fecha' y 'maquina' son obligatorios.");

                // Verificar archivo
                string rutaReporte = System.Web.Hosting.HostingEnvironment.MapPath("~/Reports/ReportsExtrusion/ReporteFechaYMaquina.rpt");
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

                // Establecer parámetros requeridos
                reporte.SetParameterValue("fecha", fecha);   
                reporte.SetParameterValue("maquina", maquina);

                // Exportar a PDF
                Stream pdf = reporte.ExportToStream(ExportFormatType.PortableDocFormat);
                pdf.Seek(0, SeekOrigin.Begin);

                // Crear nombre único de archivo con timestamp
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"Maquina_{maquina}_{timestamp}.pdf";

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StreamContent(pdf)
                };
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("inline")
                {
                    FileName = $"Reporte_{fecha}_{maquina}.pdf"
                };

                return response;
            }
            catch (Exception ex)
            {
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

