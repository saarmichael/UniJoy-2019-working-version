using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Params
{
    /// <summary>
    /// Class used to save the parameters for the rat house and the landscape house.
    /// </summary>
    public class Param
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Param()
        {

        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="par">The Param to be copied.</param>
        public Param(Param par)
        {
            _MoogParameter = par._MoogParameter;
        }

        /// <summary>
        /// The Moog parameter values for one attribute of one variable.
        /// </summary>
        public string _MoogParameter
        {
            get;
            set;
        }
    }
}
