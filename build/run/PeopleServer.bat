echo "RUN ASC.People"
call dotnet run --project ..\..\products\ASC.People\Server\ASC.People.csproj --no-build --$STORAGE_ROOT=..\..\..\Data --log__dir=..\..\..\Logs --log__name=people