using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

namespace CodeProxy.Http
{
    public class HttpApiFactory<T> where T : class, IHttpApiClient
    {
        private MethodInfo _asyncInvoker;
        private readonly IHttpClient _httpClient;
        private readonly MethodBinder _methodBinder;
        private readonly IMediaSerializer _mediaSerialiser;

        public HttpApiFactory(IHttpClient httpClient = null, IMediaSerializer mediaSerialiser = null)
        {
            _httpClient = httpClient ?? new DefaultHttpClient();
            _mediaSerialiser = mediaSerialiser;
            _methodBinder = new MethodBinder();

            var myMethods = GetType().GetTypeInfo().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);

            _asyncInvoker = myMethods.Where(n => n.Name.StartsWith("InvokeServiceTypedAsync")).FirstOrDefault();
        }

        public T Create(Uri apiBaseUri)
        {
            var classFactory = new ClassFactory<T>();
            var headers = new Dictionary<string, string[]>();
            
            classFactory.AddPropertyGetter(x => x.GlobalHeaders, (i, p, v) => headers);

            classFactory.AddMethodImplementation(InvokeService);

            var instance = classFactory.CreateInstance();

            instance.BaseUri = apiBaseUri;

            return instance;
        }

        private object InvokeService(T instance, MethodInfo method, IDictionary<string, object> parameters)
        {
            var isAsync = typeof(Task).GetTypeInfo().IsAssignableFrom(method.ReturnType);

            if (isAsync) return InvokeServiceAsync(instance, method, parameters);
            else return InvokeServiceSync(instance, method, parameters);
        }

        private object InvokeServiceSync(T instance, MethodInfo method, IDictionary<string, object> parameters)
        {
            var result = InvokeServiceAsync(instance, method, parameters);

            var resultType = result.GetType().GetTypeInfo();

            result.Wait(instance.DefaultTimeout);

            if (resultType.IsGenericType) return resultType.GetProperty("Result").GetValue(result);

            return new object();
        }

        private Task InvokeServiceAsync(T instance, MethodInfo method, IDictionary<string, object> parameters)
        {
            var returnType = GetInnerTaskType(method.ReturnType);

            if (returnType == typeof(void)) return InvokeServiceVoidAsync(instance, method, parameters);

            var genInvoker = _asyncInvoker.MakeGenericMethod(returnType);

            return genInvoker.Invoke(this, new object[] { instance, method, parameters }) as Task;
        }

        private async Task<R> InvokeServiceTypedAsync<R>(T instance, MethodInfo method, IDictionary<string, object> parameters)
        {
            var httpRequest = _methodBinder.Bind(instance, method, parameters);            

            var response = await _httpClient.SendAsync(httpRequest);

            var stream = await response.Content.ReadAsStreamAsync();

            var responseData = new HttpResponseData()
            {
                Content = stream,
                Encoding = Encoding.GetEncoding(response.Content.Headers.ContentEncoding.First()),
                MimeType = response.Content.Headers.ContentType.MediaType
            };

            return _mediaSerialiser.Deserialize<R>(responseData.MimeType, responseData.Encoding, stream);
        }

        private async Task InvokeServiceVoidAsync(T instance, MethodInfo method, IDictionary<string, object> parameters)
        {
            var httpRequest = _methodBinder.Bind(instance, method, parameters);

            var response = await _httpClient.SendAsync(httpRequest);

            var stream = await response.Content.ReadAsStreamAsync();
        }

        private Type GetInnerTaskType(Type taskType)
        {
            if (!taskType.GetTypeInfo().IsGenericType) return typeof(void);

            return taskType.GetTypeInfo().GetGenericArguments().First();
        }
    }
}