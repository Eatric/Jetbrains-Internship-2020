using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CIServerBlazor.Data
{
	public class WorkerDto
	{
		public string Url { get; set; }
		public string Branch { get; set; }
		public string Build { get; set; }
	}
}
