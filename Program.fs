// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open FSharp.Data

type CsvType = CsvProvider<"Name,Cir,CirLink", Separators=",", HasHeaders=false>

[<EntryPoint>]
let main argv =
    let now =
        sprintf "%i.%i.%i" (System.DateTime.Now).Year (System.DateTime.Now).Month (System.DateTime.Now).Day

    let marketDatas =
        [ "https://dart.fss.or.kr/dsac001/mainY.do", "Y", "Securities Market"
          "https://dart.fss.or.kr/dsac001/mainK.do", "K", "KOSDAQ Market"
          "https://dart.fss.or.kr/dsac001/mainN.do", "N", "KONEX Market" ]

    let reqUrl =
        "https://dart.fss.or.kr/dsac001/search.ax"

    let prefixURL = "https://dart.fss.or.kr"
    let mutable rows = []

    for (pageUrl, pageGroup, marketName) in marketDatas do
        let results = HtmlDocument.Load(pageUrl)

        let pageLinks =
            results.Descendants [ "div" ]
            |> Seq.choose
                (fun x ->
                    x.TryGetAttribute("class")
                    |> Option.filter (fun a -> a.Value().Equals("pageInfo"))
                    |> Option.map (fun a -> x.InnerText(), a.Value()))
            |> Seq.toList
            |> List.map (fun (name, _) -> name.Split [| '/' |])
            |> List.item 0

        let searchResults = Seq.toList pageLinks.[1]
        let lastPage = searchResults.[0] |> string |> int

        let allResult =
            searchResults.[4..searchResults.Length - 2]
            |> List.toArray
            |> System.String

        rows <-
            [ CsvType.Row(marketName, now, allResult) ]
            |> List.append rows

        for page in 1 .. lastPage do
            printfn "Scrapping Dart %s: Page %i" marketName page

            let req =
                Http.RequestString(
                    reqUrl,
                    body =
                        FormValues [ "selectDate", now
                                     "currentPage", page |> string
                                     "pageGrouping", pageGroup
                                     "mdayCnt", "1" ]
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
