using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MGRDTesting
{
    public static class InvocationHelper
    {

        /// <summary>
        /// Logger object used to display data in a console or file.
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static void InvokeIfRequired(ContainerControl target, Action del)
        {
            if (null == target)
            {
                throw new ArgumentNullException("target");
            }
            if (null == del)
            {
                throw new ArgumentNullException("del");
            }

            if (target.InvokeRequired && (!target.Disposing) && (!target.IsDisposed))
            {
                try
                {
                    target.BeginInvoke(del);
                }
                catch (Exception ex)
                {
                    Log.Info("Could not invoke method", ex);
                }
                
            }
            else
            {
                del();
            }
        }

        public static void InvokeIfRequired(ContainerControl target, Action[] del)
        {
            if (null == target)
            {
                throw new ArgumentNullException("target");
            }
            if (null == del || del.Length == 0)
            {
                throw new ArgumentNullException("del");
            }

            foreach (Action act in del)
            {
                InvokeIfRequired(target, act);
            }
        }
    }
}
