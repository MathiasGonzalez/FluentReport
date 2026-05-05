using FluentReport.Samples;

string outputDir = args.Length > 0 ? args[0] : "output";
Directory.CreateDirectory(outputDir);

// ── PDF samples ──────────────────────────────────────────────────────────────
Sample01HelloWorld.Generate(outputDir);
Sample02ReportWithTable.Generate(outputDir);
Sample03MultiPage.Generate(outputDir);
Sample04LayoutShowcase.Generate(outputDir);
Sample05Invoice.Generate(outputDir);
Sample06ThermalInvoice.Generate(outputDir);

// ── Excel samples ────────────────────────────────────────────────────────────
Sample07ExcelHelloWorld.Generate(outputDir);
Sample08ExcelReportWithTable.Generate(outputDir);
Sample09ExcelMultiSheet.Generate(outputDir);

// ── Uruguayan fiscal / legal documents ──────────────────────────────────────
Sample10ReciboSueldo.Generate(outputDir);
Sample11RemitoEntrega.Generate(outputDir);
Sample12ReciboPago.Generate(outputDir);

Console.WriteLine($"\nAll sample files written to: {Path.GetFullPath(outputDir)}");
