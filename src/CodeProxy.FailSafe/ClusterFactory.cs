using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CodeProxy.FailSafe
{
    public sealed class ClusterFactory
    {
        public Cluster<T> Create<T> (IEnumerable<T> components) where T : class
        {
            var classFactory = new ClassFactory<T>();

            var cluster = new Cluster<T>(classFactory.CreateInstance());

            classFactory.AddPropertyGetter((i, p, v) =>
            {
                foreach (var component in components)
                {
                    try
                    {
                        return p.GetValue(component);
                    }
                    catch (Exception ex)
                    {
                        cluster.RaiseError(component, ex);
                    }
                }

                throw ClusterFailedException();
            });

            classFactory.AddMethodImplementation(
                MethodFilters.NonAsyncMethods, 
                (i, m, a) =>
            {
                foreach (var component in components)
                {
                    try
                    {
                        var x = Bind<T>(m, component);

                        var result = x(a);

                        return result;
                    }
                    catch (Exception ex)
                    {
                        cluster.RaiseError(component, ex);
                    }
                }

                throw ClusterFailedException();
            });

            classFactory.AddAsyncMethodImplementation(
                async (m, a) =>
            {
                foreach (var component in components)
                {
                    try
                    {
                        var x = Bind<T>(m, component);

                        var task = (Task)x(a);
                        
                        await task;

                        var taskType = m.ReturnType.GetGenericArguments().FirstOrDefault();
                        
                        var result = task.GetTaskResult();

                        return new GenericTaskResult(task, result, taskType);
                    }
                    catch (Exception ex)
                    {
                        cluster.RaiseError(component, ex);
                    }
                }

                throw ClusterFailedException();
            });

            return cluster;
        }

        private Func<IDictionary<string, object>, object> Bind<T>(MethodInfo method, object instance)
        {
            return p => method.Invoke(instance, method.GetParameters().Join(p, o => o.Name, i => i.Key, (o, i) => i.Value).ToArray());
        }

        private object GetTaskResult(Task task)
        {
            const string resultProperty = "Result";

            var rprop = task.GetType().GetProperty(resultProperty);

            return rprop.GetValue(task);
        }

        private Exception ClusterFailedException()
        {
            return new Exception("Cluster failed");
        }
    }
}