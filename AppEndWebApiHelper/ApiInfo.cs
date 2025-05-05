using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppEndWebApiHelper
{
	public record ApiInfo(string RequestPath, string ControllerName, string ActionName)
	{
		public string RequestPath { get; set; } = RequestPath;
		public string ControllerName { get; set; } = ControllerName;
		public string ActionName { get; set; } = ActionName;
	}
}
