// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open FSharp.Data

type CsvType = CsvProvider<"Name,Cir,CirLink", Separators=",", HasHeaders=true>

[<EntryPoint>]
let main argv =
    let url =
        "https://dart.fss.or.kr/dsac001/mainY.do?selectDate=2021.10.14"

    let prefixURL = "https://dart.fss.or.kr"

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

    let mutable companyDataArr = [||]
    let mutable arrDatas = datas |> Seq.toArray
    let datasLength = datas |> Seq.length

    for i in 0 .. datasLength - 1 do
        let name, _ = arrDatas.[i]
        let trimName = name.Trim()

        if
            trimName.Equals("주주총회소집공고")
            || trimName.Equals("[기재정정]주주총회소집공고")
        then
            // Corporate opening information (href)
            let companyName, _ = arrDatas.[i - 1]
            let trimCompanyName = companyName.Trim()
            // Cir
            let cir, cirHref = arrDatas.[i]
            let trimCirName = cir.Trim()

            let companyData =
                [ trimCompanyName, trimCirName, prefixURL + cirHref ]

            companyDataArr <-
                companyData
                |> List.toArray
                |> Array.append companyDataArr

    let csv = CsvType.GetSample().Truncate(0)
    let mutable rows = []

    for (name, cir, cirLink) in companyDataArr do
        rows <-
            [ CsvType.Row(name, cir, cirLink) ]
            |> List.append rows

    let csvFile = csv.Append rows
    csvFile.Save("./company.csv")

    0
