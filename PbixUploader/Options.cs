using CommandLine;
using System;

namespace PbixUploader
{
    public class Options
    {
        [Option('c', "clientid", Required = true, HelpText = "Client ID")]
        public string ClientId { get; set; }

        [Option('w', "workspace", Required = true, HelpText = "Workspace Name")]
        public string Workspace { get; set; }

        [Option('u', "uname", Required = true, HelpText = "Username of Power BI account")]
        public string Username { get; set; }

        [Option('p', "passwd", Required = true, HelpText = "Password of Power BI account")]
        public string Password { get; set; }

        [Option('r', "reportPath", Required = true, HelpText = "Path of the PBIX file")]
        public string ReportPath { get; set; }
    }
}
