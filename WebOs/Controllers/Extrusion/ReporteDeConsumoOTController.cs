using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;

namespace WebOs.Controllers
{
    public class ReporteDeConsumoOTController : ApiController
    {
        private readonly string _storagePath = @"\\LEX\Users\DESARROLLOS\Documents\CrystalReports\Extrusion";

        //[HttpGet]
        //[Route("api/consumoOT")]
        //public HttpResponseMessage GenerarPorOT(string ot)
        //{
        //    try
        //    {
        //        // Validar parámetro
        //        if (!int.TryParse(ot, out int otNumerico))
        //            return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "❌ El parámetro 'OT' debe ser numérico.");

        //        // Verificar archivo .rpt
        //        string rutaReporte = System.Web.Hosting.HostingEnvironment.MapPath("~/Reports/ReportsExtrusion/ReporteConsumoOT.rpt");
        //        if (!File.Exists(rutaReporte))
        //            return Request.CreateErrorResponse(HttpStatusCode.NotFound, $"❌ No se encontró el reporte en: {rutaReporte}");

        //        // Cargar reporte
        //        ReportDocument reporte = new ReportDocument();
        //        reporte.Load(rutaReporte);
        //        reporte.Refresh();

        //        // Conexión a la BD
        //        ConnectionInfo conn = new ConnectionInfo
        //        {
        //            ServerName = "172.16.10.113,1433",
        //            DatabaseName = "bioflex_sap",
        //            UserID = "User_PBI",
        //            Password = "PBI*2025",
        //            IntegratedSecurity = false
        //        };

        //        foreach (Table table in reporte.Database.Tables)
        //        {
        //            TableLogOnInfo logonInfo = table.LogOnInfo;
        //            logonInfo.ConnectionInfo = conn;
        //            table.ApplyLogOnInfo(logonInfo);
        //            table.Location = $"{conn.DatabaseName}.dbo.{table.Name}";
        //        }

        //        // Asignar parámetro OT
        //        reporte.SetParameterValue("OT", otNumerico);

        //        // Crear nombre único con timestamp
        //        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        //        string fileName = $"ConsumoOT_{otNumerico}_{timestamp}.pdf";

        //        // Asegurar que la carpeta de destino existe
        //        if (!Directory.Exists(_storagePath))
        //            Directory.CreateDirectory(_storagePath);

        //        // Ruta completa donde guardar el PDF
        //        string rutaDestino = Path.Combine(_storagePath, fileName);

        //        // Exportar a disco
        //        reporte.ExportToDisk(ExportFormatType.PortableDocFormat, rutaDestino);

        //        // Leer el archivo para enviarlo al cliente
        //        Stream pdf = new FileStream(rutaDestino, FileMode.Open, FileAccess.Read);
        //        pdf.Seek(0, SeekOrigin.Begin);

        //        var response = new HttpResponseMessage(HttpStatusCode.OK)
        //        {
        //            Content = new StreamContent(pdf)
        //        };
        //        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        //        response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("inline")
        //        {
        //            FileName = fileName
        //        };

        //        return response;
        //    }
        //    catch (Exception ex)
        //    {
        //        // Manejo de errores comunes
        //        string msg = ex.Message.ToLower();

        //        if (msg.Contains("logon failed") || msg.Contains("no se pudo conectar"))
        //        {
        //            return Request.CreateErrorResponse(HttpStatusCode.Unauthorized,
        //                "❌ No se pudo conectar con la base de datos. Verifica usuario, clave o permisos.");
        //        }

        //        if (msg.Contains("missing parameter values") || msg.Contains("parámetro"))
        //        {
        //            return Request.CreateErrorResponse(HttpStatusCode.BadRequest,
        //                "❌ Faltan parámetros requeridos en el reporte.");
        //        }

        //        return Request.CreateErrorResponse(HttpStatusCode.InternalServerError,
        //            $"❌ Error inesperado al generar el reporte.\n➡ {ex.Message}");
        //    }
        //}

        [HttpGet]
        [Route("api/consumoOT")]
        public IHttpActionResult GenerarPorOT(string ot)
        {
            try
            {
                // Validar parámetro
                if (!int.TryParse(ot, out int otNumerico))
                    return BadRequest("❌ El parámetro 'OT' debe ser numérico.");

                // Verificar existencia del archivo .rpt
                string rutaReporte = System.Web.Hosting.HostingEnvironment.MapPath("~/Reports/ReportsExtrusion/ReporteConsumoOT.rpt");
                if (!File.Exists(rutaReporte))
                    return Content(HttpStatusCode.NotFound, $"❌ No se encontró el reporte en: {rutaReporte}");

                // Cargar reporte
                ReportDocument reporte = new ReportDocument();
                reporte.Load(rutaReporte);
                reporte.Refresh();

                // Conexión a la base de datos
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

                // Asignar parámetro OT
                reporte.SetParameterValue("OT", otNumerico);

                // Crear nombre único del archivo
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"ConsumoOT_{otNumerico}_{timestamp}.pdf";

                // Asegurar que la carpeta de destino existe
                if (!Directory.Exists(_storagePath))
                    Directory.CreateDirectory(_storagePath);

                // Ruta donde guardar el PDF
                string rutaDestino = Path.Combine(_storagePath, fileName);

                // Exportar el reporte a disco
                reporte.ExportToDisk(ExportFormatType.PortableDocFormat, rutaDestino);

                // Liberar recursos
                reporte.Close();
                reporte.Dispose();

                // Devolver status 200 OK con mensaje de éxito
                return Ok(new { message = "✅ Reporte generado exitosamente.", archivo = fileName });
            }
            catch (Exception ex)
            {
                string msg = ex.Message.ToLower();

                if (msg.Contains("logon failed") || msg.Contains("no se pudo conectar"))
                {
                    return Content(HttpStatusCode.Unauthorized,
                        "❌ No se pudo conectar con la base de datos. Verifica usuario, clave o permisos.");
                }

                if (msg.Contains("missing parameter values") || msg.Contains("parámetro"))
                {
                    return BadRequest("❌ Faltan parámetros requeridos en el reporte.");
                }

                return InternalServerError(new Exception($"❌ Error inesperado al generar el reporte.\n➡ {ex.Message}"));
            }
        }


    }
}
