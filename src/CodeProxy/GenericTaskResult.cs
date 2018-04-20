using System;
using System.Threading.Tasks;

namespace CodeProxy
{
    public sealed class GenericTaskResult
    {
        public GenericTaskResult(Task task, object result, Type resultType)
        {
            Task = task;
            ResultType = resultType;
            Result = result;
        }

        public Task Task { get; }

        public object Result { get; }

        public Type ResultType { get; }

        public Task ConvertResult() => Result.ConvertToTask(ResultType);
    }
}