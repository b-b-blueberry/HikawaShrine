﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hikawa
{
	public interface ISailorStylesAPI
	{
		bool AreHairstylesEnabled();
		int GetHairstylesInitialIndex();
	}
}