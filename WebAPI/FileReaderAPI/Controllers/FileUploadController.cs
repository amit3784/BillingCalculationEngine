using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data;
using System.Text.Json.Serialization;

namespace FileReaderAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    
    public class FileUploadController : ControllerBase
    {
        [HttpPost]
        public  IActionResult PostFileUpload(IFormFile file)
        {
            var filename = file.FileName;
            var filePath = Path.Combine(Environment.CurrentDirectory, filename);
            using (Stream filestram = new FileStream(filePath, FileMode.OpenOrCreate))
            {
                 file.CopyToAsync(filestram);
            }
            Dictionary<string, DataTable> dtDict = ReadExcelToDataTable(filePath);



            var dtFilteredforC001 =( from dtclient in dtDict["client_billing"].AsEnumerable()
                                    join dtportfolio in dtDict["portfolio"].AsEnumerable()
                                    on dtclient["Client ID"] equals dtportfolio["Client ID"]
                                    //join dtbillingtier in dtDict["billing_tier"].AsEnumerable()
                                    //on dtclient["Billing Tier ID"] equals dtbillingtier["Tier ID"]
                                    join dtassets in dtDict["assets"].AsEnumerable()
                                    on dtportfolio["Portfolio ID"] equals dtassets["Portfolio ID"]
                                    orderby dtclient["Client ID"]
                                    select new
                                    {
                                        ClientId = dtclient.Field<string>("Client ID"),
                                        //ClientName = dtclient.Field<string>("Client Name"),
                                        //Province = dtclient.Field<string>("Province"),
                                        //Country = dtclient.Field<string>("Country"),
                                        PortfolioId = dtportfolio.Field<string>("Portfolio ID"),
                                        PortfolioCurrency = dtportfolio.Field<string>("Portfolio Currency"),
                                        Date = dtassets.Field<string>("Date"),
                                        //AssetId = dtassets.Field<string>("Asset ID"),
                                        AssetValue =(dtassets.Field<string>("Currency") == "USD")?Convert.ToString((Convert.ToDouble(dtassets.Field<string>("Asset Value"))/0.71)) : dtassets.Field<string>("Asset Value"),
                                        AssetCurrency = dtassets.Field<string>("Currency"),
                                        TierId = dtclient.Field<string>("Billing Tier ID")
                                        
                                    }).ToList();
            var groupedResult = (from res in dtFilteredforC001.AsEnumerable()
                                 group res by  new { res.ClientId, res.PortfolioId, res.TierId } into grouped
                                 select new
                                 {
                                     clientid=grouped.Key.ClientId,
                                     portfolioid=grouped.Key.PortfolioId,
                                     tierid=grouped.Key.TierId,
                                     assetvalue=grouped.Sum(x=>Convert.ToDecimal(x.AssetValue))
                                 }
                               )
                               .ToList();
            DataTable dtComposedResult = new DataTable();
            DataColumn clientidColumn = new DataColumn("clientid");
            dtComposedResult.Columns.Add(clientidColumn);
            DataColumn portfolioidColumns = new DataColumn("portfolioid");
            dtComposedResult.Columns.Add(portfolioidColumns);
            DataColumn tierIdColumn = new DataColumn("tierid");
            dtComposedResult.Columns.Add(tierIdColumn);
            DataColumn totalAssetValueColumn = new DataColumn("assetvalue");
            dtComposedResult.Columns.Add(totalAssetValueColumn);
            DataColumn feesColumn = new DataColumn("fees");
            dtComposedResult.Columns.Add(feesColumn);
            DataColumn feepercentageColumn = new DataColumn("feepercentage");
            dtComposedResult.Columns.Add(feepercentageColumn);
            foreach ( var dr in groupedResult.AsEnumerable() )
            {
                decimal assetValue = Convert.ToDecimal(dr.assetvalue);
                decimal fees = 0;
                decimal feepercentage = 0;
                decimal difference = 0;
                DataRow drnew = dtComposedResult.NewRow();
                drnew["clientid"] =dr.clientid;
                drnew["portfolioid"]=dr.portfolioid;
                drnew["tierid"]=dr.tierid;
                drnew["assetvalue"]=dr.assetvalue;

                foreach (DataRow dataRow in dtDict["billing_tier"].AsEnumerable().Where(x=>x.Field<string>("Tier ID")==dr.tierid))
                {
                    
                    if (assetValue >Convert.ToDecimal( dataRow["Portfolio AUM Max ($)"]))
                    {
                        difference = (Convert.ToDecimal(dataRow["Portfolio AUM Max ($)"])) - (Convert.ToDecimal(dataRow["Portfolio AUM Min ($)"]));
                        fees += (difference) * Math.Round((Convert.ToDecimal(dataRow["Fee Percentage (%)"])),6);
                       // assetValue = assetValue - Convert.ToDouble(dataRow["Portfolio AUM Max ($)"]);
                    }
                    else
                    {
                        if (assetValue > Convert.ToDecimal(dataRow["Portfolio AUM Min ($)"]))
                        {
                            difference = assetValue - Convert.ToDecimal(dataRow["Portfolio AUM Min ($)"]);
                        }
                           
                        fees += (difference) * (Convert.ToDecimal(dataRow["Fee Percentage (%)"]));
                        feepercentage =Convert.ToDecimal(Math.Round((fees * 100 / Convert.ToDecimal(dr.assetvalue)) ,6));
                        
                        //DataColumn columnfees = new DataColumn("fees");
                        //dtDict["billing_tier"].Columns.Add(columnfees);
                        //dataRow["fees"]=fees;
                        //DataColumn columnFeePercentage = new DataColumn("feepercentage");
                        //dtDict["billing_tier"].Columns.Add(columnFeePercentage);
                        //dataRow["columnFeePercentage"] = feepercentage;
                    }
                }
                drnew["fees"] = fees;
                drnew["feepercentage"] = feepercentage;
                dtComposedResult.Rows.Add(drnew);
            }

           var result = JsonConvert.SerializeObject(dtComposedResult);
            return Ok(result);
          
        }
        private Dictionary<string, DataTable> ReadExcelToDataTable(string filePath)
        {

            Dictionary<string, DataTable> dtDict = new Dictionary<string, DataTable>();
            using (var workbook = new XLWorkbook(filePath))
            {
                foreach (var worksheet in workbook.Worksheets)
                {
                    var dataTable = new DataTable(worksheet.Name);
                    var firstRow = true;
                    foreach (var row in worksheet.RowsUsed())
                    {
                        if (firstRow)
                        {
                            foreach (var cell in row.Cells())
                            {
                                dataTable.Columns.Add(cell.Value.ToString());
                            }
                            firstRow = false;
                        }
                        else
                        {
                            dataTable.Rows.Add();
                            var i = 0;
                            foreach (var cell in row.Cells())
                            {
                                dataTable.Rows[dataTable.Rows.Count - 1][i] = cell.Value.ToString();
                                i++;
                            }
                        }
                    }
                    dtDict.Add(worksheet.Name, dataTable);
                }

            }
            return dtDict;
        }
    }
}
