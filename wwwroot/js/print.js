window.openPrintPreview = (htmlContent) => {
    const tab = window.open('', '_blank');
    tab.document.write(htmlContent);
    tab.document.close();
};