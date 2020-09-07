lexer grammar RealtyTypeLexer;
import  BaseLexer;

fragment KOMNATA : 
      'комнаты' 
    | 'комната'
;

fragment KOMNATNAYA : 
       'комнатная' 
    | 'комн.'
;

fragment KOMN_NUMBER : 
                  '1-x' | '1x-' | '1' | 'одно'  | '1-но'
                | '2-x' | '2x-' | '2' | '2х'| 'двух'
                | '3-x' | '3x-' | '3' | '3х'| 'трех' | 'трёх'
                | '4-x' | '4x-' | '4' | '4х'| 'четырех'
                | '5-x' | '5x-' | '5' | '5х'| 'пяти'
                | '6-x' | '6x-' | '6' | '6х'| 'шести'
;

fragment KVARTIRA : 
            'квартира'
        |   'квартиры'
        |   'квартра'
        |   'квртира'
        |   'квар-тира'
        |   (KOMN_NUMBER ' '? KOMNATNAYA ' квартира')
        |   ('квартира ' KOMN_NUMBER ' '? KOMNATNAYA)
        |   ('квартира'  ' '?    '(' KOMNATA  ' '  [1-9,]+ ')' ) /*Квартира (комнаты 1,2)*/
        

        ;




fragment ZEM_UCHASTOK: 
              ('земельный участок' [.,]?)
            | 'участок'
            | 'зем. участок'
            | 'зем.участок'
            | 'земель-ный участок'
            | 'зем. уч.'
            | 'зем уч-к'
            | 'земельн. участок'
            | 'земля'
            | 'земли'
            | 'земельные участки'
            | 'земельный участок.'
            | 'земельный уч-к'
            | 'земельный учаток'
            | 'земел. участок'
            | 'земельн.участок'
            | 'земельн участок'
            | 'индивидуальный земельный участок'
;


fragment PREPOSITION : 
              'для ведения'
            | 'под'
            | 'по'
            | 'для'
            | 'для размещения'
            | 'для строительства'
            | 'для эксплуатации'
            | 'под эксплуатацию'
;


/* для группы "под жилым домом" */
fragment DOM :            
      'жилым домом'
    | 'жилого дома'
    | 'индивидуальным жилым домом'

    | 'многоквартирным домом'
    | 'многоквартирного дома'
    | 'многоквартирным жилым домом'
    | 'многоквартирного жилого дома'

    | 'домом'
    | 'дома'
    | 'домов индивидуальной жилой застройки'
    | 'домов индивидуальной застройки'

    | 'гаражным боксом'
    | 'гаражного бокса'
    | 'гаража'
    | 'гараж'
    | 'гаражом'
    | 'гаражей и автостоянок'
    | 'объектов торговли'

    | 'хозяйственными постройками'
    | 'хозпостройки'
;



fragment SELCHOZ: 
      'сельскохозяйственного'
    | 'сельскохозяй- ственного'
    | 'с/х'
    | 'сельхоз'
    | 'сельхоз.'
    | 'сельхоз-'
;

fragment ZEM_UCHASTOK_ADJ:
              'огородный'
            | 'дачный'
            | 'приусадебный'
            | 'садовый'
            | 'приусадеб-ный'
            | 'садово-огородный'
            | 'полевой'
            | SELCHOZ
;

fragment ZEM_UCHASTOK_PURPOSE:
                   ('ижс')
                |  ('индивидуально' ('го'|'е') ' жилищно' . .? ' строительств' .)
                |  'жилищного строительства'
                |  'индивид. жил. строит.'
                |  'индивидуальное гаражное строительство'                
                |  'индивидуаль-ное жилищное строительство'

                |  ('лпх')
                |  ('лично' . .? ' подсобно' . .? ' хозяйств' . )

                |  ('мкд')
                
                | (SELCHOZ ' '? 'использования')
                | (SELCHOZ ' '? 'назначения')
                | (SELCHOZ ' '? 'производства')
                | (SELCHOZ)
                
                | ('садоводств' .)
                | ('садоводств' . ' и огородничеств' .)

                | ', находящийся в составе дачных, садоводческих и огороднических объединений'
                | 'строительство'
                    
                | 'дачного хозяйства'
            ;

fragment ZEM_UCHASTOK_WITH_PROPS: 
              ZEM_UCHASTOK  
            | (ZEM_UCHASTOK ' ' (PREPOSITION ' ')? ZEM_UCHASTOK_PURPOSE)
            | (ZEM_UCHASTOK_ADJ ' ' (ZEM_UCHASTOK_ADJ ' ')? ZEM_UCHASTOK)
            | (ZEM_UCHASTOK ' ' ZEM_UCHASTOK_ADJ)
            | ('земельный ' ZEM_UCHASTOK_ADJ ' участок')
            | (ZEM_UCHASTOK_ADJ ' участок')
            | (ZEM_UCHASTOK ' ' PREPOSITION ' ' DOM)  /* обязательный предлог */
;


REALTY_TYPE :   

  KVARTIRA
| ZEM_UCHASTOK_WITH_PROPS
| (FRACTION_UNICODE ' ' DOM)
| 'жилой дом'
| 'гараж'
| 'дом'
| 'комната'
| 'нежилое помещение'
| 'дача'
| 'садовый участок'
| 'садовый дом'
| 'гаражный бокс'
| 'дачный дом'
| 'нежилое здание'
| 'иное недвижимое имущество'
| 'приусадебный участок'
| 'баня'
| 'комната в общежитии'
| 'земельный пай'
| 'часть жилого дома'
| 'машиноместо'
| 'садовый домик'
| 'сарай'
| 'объект незавершенного строительства'
| 'машино-место'
| 'гараж-бокс'
| 'жилое помещение'
| 'общежитие'
| 'помещение'
| 'жилое строение'
| 'здание'
| 'хозяйственное строение'
| 'магазин'
| 'служебная квартира'
| 'нежилое строение'
| 'хозяйственная постройка'
| 'частный дом'
| 'летняя кухня'
| 'комната в квартире'
| 'дачный домик'
| 'погреб'
| 'нежилой дом'
| 'здание магазина'
| 'жилой дом с хозяйственными постройками'
| 'склад'
| 'парковочное место'
| 'пай'
| 'индивидуальный жилой дом'
| 'бокс'
| 'хозблок'
| 'гаражи'
| 'дом дачный'
| 'здание нежилое'
| 'помещение нежилое'
| 'жилые дома:'
| 'домовладение'
| 'огородный участок'
| 'жилая квартира'
| 'лпх'
| 'капитальный гараж'
| 'жилищное строительство'
| 'земли населенных пунктов'
| 'незавершенное строительство'
| 'квартира служебная'
| 'жилой дом с надворными постройками'
| 'одноэтажный жилой дом'
| 'административное здание'
| 'общее имущество в многоквартирном доме'
| 'коттедж'
| 'кладовая'
| 'автостоянка'
| 'овощехранилище'
| 'подвал'
| 'строение'
| 'земли поселений'
| 'индивидуальное жилищное строительство'
| 'незавершенный строительством жилой дом'
| 'здание склада'
| 'дом нежилой'
| 'торговый павильон'
| 'хозяйственный блок'
| 'гараж с подвалом'
| 'паркинг'
| 'сток'
| 'кладовка'
| 'жил.дом'
| 'надворные постройки'
| 'стояночное место'
| 'лесной участок'
| 'изолированная часть жилого дома'
| 'дом садовый'
| 'часть дома'
| 'машино место'
| 'погребная ячейка'
| 'недостроенный жилой дом'
| 'навес'
| 'встроенное нежилое помещение'
| 'хозпостройка'
| 'подземная автостоянка'
| 'гараж-стоянка'
| 'беседка'
| 'офисное помещение'
| 'жилой дом с хозяйственными строениями'
| 'жилое строение без права регистрации'
| 'бокс гаража'
| 'личное подсобное хозяйство'
| 'жилойдом'
| 'жилое помещение в общежитии'
| 'паевые земли'
| 'гараж металлический'
| 'овощная яма'
| 'котельная'
| 'земля сельскохозяйственного назначения'
| 'комнаты в общежитии'
| 'служебное помещение'
| 'овощная кладовка'
| 'коммунальная квартира'
| 'хоз. строение'
| 'незавершенное строительство жилого дома'
| 'комната в жилом доме'
| 'муниципальная квартира'
| 'нежилое встроенное помещение'
| 'комната в общежитие'
| 'кухня'
| 'хоз. постройка'
| 'земли сельхоз назначения'
| '3-комнатная'
| 'зем.участок под ижс'
| 'металлический гараж'
| 'гараж кирпичный'
| 'гараж с погребом'
| 'апартаменты'
| 'жилой жом'
| 'земля под гараж'
| 'домик'
| 'встроенное помещение'
| 'объект незавершенный строительством'
| 'хозяйственное помещение'
| 'недостроенный дом'
| 'эллинг'
| 'ячейка в овощехранилище'
| 'подвальное помещение'
| 'часть здания'
| 'теплица'
| 'павильон'
| 'хоз.строение'
| 'койко-место'
| 'складское помещение'
| 'коровник'
| 'парковка'
| 'хоз.блок'
| 'квартира в общежитии'
| 'жилой дом одноэтажный'
| 'торговое помещение'
| 'служебный жилой дом'
| 'таунхаус'
| 'помещение магазина'
| 'производственное здание'
| 'летний дом'
| 'жилой дом с мансардой'
| 'домжилой'
| 'летний домик'
| 'жилой дом с пристройкой'
| 'хоз.постройка'
| 'хоз. блок'
| 'овощная ячейка'
| 'жилое строение на садовом участке'
| 'гаражный бокс с подвалом'
| 'земли сельскохозяйственного назначения, для сельскохозяйственного производства'
| 'земельный участок земли поселений'
| 'мастерская'
| 'две комнаты в коммунальной квартире'
| 'часть нежилого помещения'
| 'производственное помещение'
| 'приусадебный участок к дому'
| 'земли населенных пунктов для ведения личного подсобного хозяйства'
| 'нежилое сооружение'
| 'газопровод'
| 'проходная'
| 'комната на общей кухне'
| 'жилые помещения'
| 'земельные паи'
| 'жил дом'
| 'дачное строительство'
| 'хоз. постройки'
| 'кафе'
;           
