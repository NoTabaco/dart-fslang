// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open FSharp.Data

[<EntryPoint>]
let main argv =
    let url =
        "https://dart.fss.or.kr/dsac001/mainY.do"

    let results = HtmlDocument.Load(url)

    let links =
        results.Descendants [ "a" ]
        |> Seq.choose
            (fun x ->
                x.TryGetAttribute("href")
                |> Option.map (fun a -> x.InnerText(), a.Value()))
        |> Seq.toList

    let searchResults =
        links
        |> List.filter (fun (_, url) -> url.StartsWith("javascript:search"))
        |> List.map (fun (name, _) -> name)

    for searchResult in searchResults do
        printfn "%s" searchResult



    0
