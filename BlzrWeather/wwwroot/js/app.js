window.AvaWeather = {
    setLanguage: language => { document.documentElement.lang = language; },
    setThemeColor: color => {
        let tag = document.querySelector('meta[name="theme-color"]');
        if (!tag) {
            tag = document.createElement('meta');
            tag.name = 'theme-color';
            document.head.appendChild(tag);
        }
        tag.content = color;
    }
};
