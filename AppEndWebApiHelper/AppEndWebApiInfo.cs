using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppEndWebApiHelper
{
	public record AppEndWebApiInfo(string requestPath, string controllerName, string actionName)
	{
		public string ControllerName { get; set; } = controllerName;
		public string ActionName { get; set; } = actionName;
		public string RequestPath { get; set; } = requestPath;
	}
}
