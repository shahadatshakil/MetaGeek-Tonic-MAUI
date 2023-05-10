using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Security;

namespace MetaGeek.Tonic.Common.Models
{
    public class ExceptionWrapper
    {
        [SecurityCritical]
        public T Wrap<T>(Func<T> func)
        {
#if !DEBUG
            try
            {
#endif
            return func();
#if !DEBUG
            }
            catch (ObjectDisposedException ex)
            {
                Trace.TraceWarning(ex.ToString());
            }
            catch (InvalidOperationException ex)
            {
                Trace.TraceWarning(ex.ToString());
            }
            catch (AccessViolationException ex)
            {
                Trace.TraceWarning(ex.ToString());
            }
            catch (NullReferenceException ex)
            {
                //TODO:This gets thrown on close for slow clients. there should be a way to avoid having to catch this here.
                Trace.TraceWarning(ex.ToString());
            }
            catch (Exception ex)
            {
                Trace.TraceWarning(ex.ToString());
            }
            return default(T);
#endif
        }

        [SecurityCritical]
        public void Wrap(Action action)
        {
#if !DEBUG
            try
            {
#endif
            action.Invoke();
#if !DEBUG
            }
            catch (InvalidOperationException ex)
            {
                Trace.TraceWarning(ex.ToString());
            }
            catch (AccessViolationException ex)
            {
                Trace.TraceWarning(ex.ToString());
            }
            catch (NullReferenceException ex)
            {
                //TODO:This gets thrown on close for slow clients. there should be a way to avoid having to catch this here.
                Trace.TraceWarning(ex.ToString());
            }
            catch (Exception ex)
            {
                Trace.TraceWarning(ex.ToString());
            }
#endif
        }
    }
}
