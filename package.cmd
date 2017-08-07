dotnet restore "src\CodeProxy\CodeProxy.csproj"
dotnet build "src\CodeProxy\CodeProxy.csproj" --configuration Release
dotnet pack "src\CodeProxy\CodeProxy.csproj" --configuration Release --output "../../artifacts"
PAUSE