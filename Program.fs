// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open FSharp.Data

type CompanyData() =
    class
        let mutable name = ""
        let mutable cir = ""
        let mutable link = ""

        member x.Insert(n, c, l) =
            name <- n
            cir <- c
            link <- l
    end


[<EntryPoint>]
let main argv =
    let url =
        "https://dart.fss.or.kr/dsac001/mainY.do?selectDate=2021.10.14"

    let prefixURL = "https://dart.fss.or.kr/"

    let results = HtmlDocument.Load(url)

    (* let pageLinks =
        results.Descendants [ "a" ]
        |> Seq.choose
            (fun x ->
                x.TryGetAttribute("href")
                |> Option.map (fun a -> x.InnerText(), a.Value()))
        |> Seq.toList

    let searchResults =
        pageLinks
        |> List.filter (fun (_, url) -> url.StartsWith("javascript:search"))
        |> List.map (fun (name, _) -> name)

    let lastPage =
        (searchResults).[searchResults.Length - 1] *)

    let datas =
        results.Descendants [ "tr" ]
        |> Seq.map (fun x -> x.Descendants [ "a" ])
        |> Seq.collect
            (fun x ->
                x
                |> Seq.choose
                    (fun x ->
                        x.TryGetAttribute("href")
                        |> Option.map (fun a -> x.InnerText(), a.Value())))

    let mutable companyData = [||]
    let mutable arrDatas = datas |> Seq.toArray
    let datasLength = datas |> Seq.length

    for i in 0 .. datasLength - 1 do
        let mutable (name, href) = arrDatas.[i]

        if name = "주주총회소집공고" then
            companyData <- [| name, href |] |> Array.append companyData
            printfn "%A" companyData

    0
