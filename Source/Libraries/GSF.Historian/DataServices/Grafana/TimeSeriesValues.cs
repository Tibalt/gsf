﻿//******************************************************************************************************
//  TimeSeriesValues.cs - Gbtc
//
//  Copyright © 2016, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may not use this
//  file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  09/12/2016 - Ritchie Carroll
//       Generated original version of source code.
//
//******************************************************************************************************

using System.Collections.Generic;

namespace GSF.Historian.DataServices.Grafana
{
    /// <summary>
    /// Defines a Grafana time-series value.
    /// </summary>
    public class TimeSeriesValues
    {
        /// <summary>
        /// Defines a Grafana time-series value point source.
        /// </summary>
        public string target;

        /// <summary>
        /// Defines a Grafana time-series value data.
        /// </summary>
        /// <remarks>
        /// "datapoints":[
        ///       [622,1450754160000],
        ///       [365,1450754220000]
        /// ]
        /// </remarks>
        public List<double[]> datapoints;
    }
}
