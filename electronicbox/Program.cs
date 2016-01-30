using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace electronicbox
{
    class Program
    {
        public static decimal CustomParse(string incomingValue)
        {
            decimal val;
            if (!decimal.TryParse(incomingValue.Replace(",", "").Replace(".", ""), NumberStyles.Number, CultureInfo.InvariantCulture, out val))
                return 0;
            return val / 100;
        }
        static void Main(string[] args)
        {
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("actions", "list"),
                new KeyValuePair<string, string>("DELETE_lng", "en"),
                new KeyValuePair<string, string>("lng", "en"),
                new KeyValuePair<string, string>("code", "YOURKEY")
            });

            Decimal pourcent = 0;

            var myHttpClient = new HttpClient();
            var response = myHttpClient.PostAsync("http://conso.ebox.ca/index.php", formContent).GetAwaiter().GetResult();
            var stringContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();

            // There are various options, set as needed
            //htmlDoc.OptionFixNestedTags = true;

            // filePath is a path to a file containing the html
            htmlDoc.LoadHtml(stringContent);

            // filePath is a path to a file containing the html
            //htmlDoc.Load("C:\\Users\\olivier\\Desktop\\index.txt");

            var allElementsWithClassTdBlock = htmlDoc.DocumentNode.SelectNodes("//*[contains(@class,'td_block')]");
            var allElementsWithClassNoBr = htmlDoc.DocumentNode.SelectNodes("//*[contains(@class,'nobr')]");

            //Total76.5 G
            var Total = allElementsWithClassTdBlock[allElementsWithClassTdBlock.Count-1].InnerText;
            
            //Plan total: 250 G
            var AllowedData = allElementsWithClassNoBr[3].InnerText;

            var sTotal = Regex.Replace(Total, @"[^0-9.,]+", "");
            var sDataMax = Regex.Replace(AllowedData, @"[^0-9.,]+", "");

            try
            {
                var dTotal = CustomParse(sTotal);
                var dDataMaxAllowed = CustomParse(sDataMax);

                if(dTotal > 0 && dDataMaxAllowed > 0)
                {
                    pourcent = (dTotal / dDataMaxAllowed);
                }

                if(pourcent > 85)
                {
                    var fromAddress = new MailAddress("YOUREMAIL", "Electronic box checker");
                    var toAddress = new MailAddress("SENDEREMAIL", "NAME");
                    const string fromPassword = "PASSWORD";
                    const string subject = "Electronic box - Limit data checker";
                    string body = "you have consumed: "+pourcent.ToString()+" %";

                    var smtp = new SmtpClient
                    {
                        Host = "smtp.gmail.com",
                        Port = 587,
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                    };
                    using (var message = new MailMessage(fromAddress, toAddress)
                    {
                        Subject = subject,
                        Body = body
                    })
                    {
                        smtp.Send(message);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            //Debug.WriteLine(pourcent);
        }
    }
}
