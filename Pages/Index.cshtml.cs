using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Models;
using System.Text;

namespace prova_drivin.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IWebHostEnvironment _environment;

        public IEnumerable<EmailsFile> EmailsFiles = new List<EmailsFile>();

        public IndexModel(ILogger<IndexModel> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        [BindProperty]
        public IFormFile UploadFile { get; set; }
        public async Task OnPostAsync()
        {
            using (var memoryStream = new MemoryStream())
            {
                await UploadFile.CopyToAsync(memoryStream);
                var fileContent = Encoding.UTF8.GetString(memoryStream.ToArray());
                var emails = ExtractEmails(fileContent);
                EmailsFiles = SepateIntoFiles(emails);
            }

            var filesFolder = Path.Combine(_environment.WebRootPath, "emailfiles");

            if(!Directory.Exists(filesFolder))
                Directory.CreateDirectory(filesFolder);

            foreach (var file in EmailsFiles)
            {
                System.IO.File.WriteAllLines($"{filesFolder}/{file.Name}", file.Emails);
            }
        }

        [HttpGet("download")]
        public  IActionResult FileDownload([FromQuery] string path)
        {
            Console.WriteLine(path);
            var net = new System.Net.WebClient();
            var data = net.DownloadData(path);
            var content = new System.IO.MemoryStream(data);
            var contentType = "text/plain";
            var fileName = path.Split("/").Last();
            
            return File(content, contentType, fileName);
        }

        private IList<string> ExtractEmails(string fileContent) 
        {
            return fileContent
                .Split("\n")
                .Select(email => email.Trim())
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .GroupBy(email => email)
                .Select(group => group.First())
                .ToList();
        }

        private IEnumerable<EmailsFile> SepateIntoFiles(IList<string> emails) 
        {
            var files = new List<EmailsFile>();
            var fileContent = new List<string>();
            var fileIndexCounter = 1;
            var numberOfEmailsByFile = 5;
            
            // Dando o crédito:
            // https://stackoverflow.com/questions/16167983/best-regular-expression-for-email-validation-in-c-sharp
            var validEmailRegex = new Regex(@"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z", RegexOptions.IgnoreCase);
            // Eu demoraria uma semana escrevendo esta regex...

            for (int i = 0; i < emails.Count; i++)
            {
                var email = emails[i];
                
                if (!validEmailRegex.IsMatch(email))
                    continue;

                fileContent.Add(email);

                if(fileContent.Count() == numberOfEmailsByFile) 
                {
                    files.Add(new EmailsFile 
                    {
                        Name = fileIndexCounter.ToString("D2") + ".txt",
                        Emails = fileContent
                    });

                    fileContent = new List<string>();
                    fileIndexCounter++;
                }    
            }

            return files;
        }

    }
}
