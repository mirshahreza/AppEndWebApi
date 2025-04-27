using AppEndCommon;
using AppEndWebApiHelper;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace NS1
{
	[Route("NS1/MyTest")]
	[ApiController]
	public class MyTestController : ControllerBase
	{
		private readonly IHttpContextAccessor _httpContextAccessor;

		public MyTestController(IHttpContextAccessor httpContextAccessor)
		{
			_httpContextAccessor = httpContextAccessor;
		}

		[HttpPost("M1")]
		public ActionResult M1([ModelBinder(typeof(AppEndJsonBinder))] int a, [ModelBinder(typeof(AppEndJsonBinder))] int b, [ModelBinder(typeof(AppEndJsonBinder))] JsonElement c)
		{
			if (c.ValueKind == JsonValueKind.Undefined) throw new ArgumentNullException(nameof(c));
			return Ok(c);
		}

		[HttpPost("M2")]
		public string? M2([ModelBinder(typeof(AppEndJsonBinder))] int a, [ModelBinder(typeof(AppEndJsonBinder))] int b, [ModelBinder(typeof(AppEndJsonBinder))] JsonElement c)
		{
			if (c.ValueKind == JsonValueKind.Undefined) throw new ArgumentNullException(nameof(c));


			return _httpContextAccessor.HttpContext?.User.Identity?.Name?.ToStringEmpty();
		}
	}
}

namespace NS2
{
	[Route("NS2/MyTest")]
	[ApiController]
	public class MyTest : ControllerBase
	{
		[HttpPost("M1")]
		public ActionResult M1([ModelBinder(typeof(AppEndJsonBinder))] int a, [ModelBinder(typeof(AppEndJsonBinder))] int b, [ModelBinder(typeof(AppEndJsonBinder))] JsonElement c)
		{
			if (c.ValueKind == JsonValueKind.Undefined) throw new ArgumentNullException(nameof(c));
			return Ok("GGGGGGGGGGGGGGGGGGGGGGG");
		}

		[HttpPost("M2")]
		public ActionResult M2([ModelBinder(typeof(AppEndJsonBinder))] int a, [ModelBinder(typeof(AppEndJsonBinder))] int b, [ModelBinder(typeof(AppEndJsonBinder))] JsonElement c)
		{
			if (c.ValueKind == JsonValueKind.Undefined) throw new ArgumentNullException(nameof(c));
			return Ok(c.ToString());
		}
	}
}

