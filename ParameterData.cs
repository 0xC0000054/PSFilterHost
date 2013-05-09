/////////////////////////////////////////////////////////////////////////////////
//
// Adobe(R) Photoshop(R) filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2013 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;

namespace PSFilterHostDll
{
    /// <summary>
    /// The class that contains the filter's global and scripting parameters.
    /// </summary>
    [Serializable]
    public sealed class ParameterData 
    {
        private readonly GlobalParameters globalParameters;
        private readonly ScriptingDataCollection scriptingData;

        /// <summary>
        /// Gets the filter's global parameters.
        /// </summary>
        public GlobalParameters GlobalParameters
        {
            get 
            {
                return this.globalParameters;
            }
        }
       
        /// <summary>
        /// Gets the collection of  AETE scripting values.
        /// </summary>
        public ScriptingDataCollection ScriptingData
        {
            get
            {
                return this.scriptingData;
            }
        }

        internal ParameterData(GlobalParameters globals, Dictionary<uint, AETEValue> aete)
        {
            this.globalParameters = globals;

            if (aete != null)
            {
                this.scriptingData = new ScriptingDataCollection(aete);
            }
            else
            {
                this.scriptingData = null;
            }
        }
       
    }
}
