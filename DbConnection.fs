module DBConnection

open FSharp.Data.Sql

let [<Literal>] SrcDir = __SOURCE_DIRECTORY__
let [<Literal>] ConnectionString = "Data Source=" + SrcDir + "\database.db;Version=3;FailIfMissing=True;"
let [<Literal>] ResolutionPath = @"%UserProfile%\.nuget\packages\System.Data.SQLite.Core\lib\netstandard2.0"

type sql =  SqlDataProvider<Common.DatabaseProviderTypes.SQLITE, 
                            SQLiteLibrary = Common.SQLiteLibrary.SystemDataSQLite,
                            ConnectionString = ConnectionString,
                            ResolutionPath = ResolutionPath,
                            IndividualsAmount = 1000,
                            UseOptionTypes = true,
                            CaseSensitivityChange = Common.CaseSensitivityChange.TOLOWER>

let DbContext = sql.GetDataContext()
let Messages = DbContext.Main.messages
