﻿popular_domains = [
"youtube.com",
"vk.com",
"google.com",
"yandex.ru",
"mail.ru",
"avito.ru",
"ok.ru",
"odnoklassniki.ru",
"google.ru",
"wikipedia.org",
"aliexpress.com",
"sberbank.ru",
"gismeteo.ru",
"kinopoisk.ru",
"rambler.ru",
"instagram.com",
"gosuslugi.ru",
"wildberries.ru",
"drom.ru",
"ozon.ru",
"userapi.com",
"ivi.ru",
"hh.ru",
"twitch.tv",
"ria.ru",
"livejournal.com",
"pikabu.ru",
"booking.com",
"facebook.com",
"lordsfilm.tv",
"gazeta.ru",
"lenta.ru",
"drive2.ru",
"fandom.com",
"2gis.ru",
"vseigru.net",
"sbrf.ru",
"youla.ru",
"kp.ru",
"rbc.ru",
"citilink.ru",
"rutracker.org",
"rt.ru",
"mvideo.ru",
"roblox.com",
"rzd.ru",
"vesti.ru",
"otzovik.com",
"championat.com",
"rp5.ru",
"ficbook.net",
"gosuslugi.ru",
"twitter.com",
"gismeteo.ru",
"telegram.org",
"web.telegram.org"
]

popular_site_set = set(popular_domains)

def is_super_popular_domain(domain):
    return domain in popular_site_set;