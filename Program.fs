// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open FSharp.Data

type CsvType = CsvProvider<"Name,Cir,CirLink", Separators=",", HasHeaders=true>

[<EntryPoint>]
let main argv =
    let pageUrl =
        "https://dart.fss.or.kr/dsac001/mainY.do"

    let reqUrl =
        "https://dart.fss.or.kr/dsac001/search.ax"

    let prefixURL = "https://dart.fss.or.kr"

    let results = HtmlDocument.Load(pageUrl)

    let pageLinks =
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
        (searchResults).[searchResults.Length - 1] |> int

    let mutable rows = []

    for page in 1 .. lastPage do
        printfn "Scrapping Dart Marketable Securities: Page %i" page

        let req =
            Http.RequestString(
                reqUrl,
                body =
                    FormValues [ "selectDate", "2021.10.15"
                                 "currentPage", page |> string
                                 "pageGrouping", "Y"
                                 "mdayCnt", "0" ]
            )

        let reqHtml = HtmlDocument.Parse(req)

        let datas =
            reqHtml.Descendants [ "tr" ]
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

        for (name, cir, cirLink) in companyDataArr do
            rows <-
                [ CsvType.Row(name, cir, cirLink) ]
                |> List.append rows

    printfn "Making Csv File..."
    let csv = CsvType.GetSample().Truncate(0)
    let csvFile = csv.Append rows
    csvFile.Save("./company.csv")
    printfn "Completed !"

    0
