from PIL import Image, ImageOps, ImageDraw, ImageFont
from pathlib import Path
root=Path(__file__).parent
names=['dashboard','requests','new-request','request-1042','clarify-1042','scope-1042','catalogue','service-ELE-001','customers','customer-1','quotations','quote-128','quote-edit-1042','quote-preview-1042','history-1042','settings','settings-access','sign-in']
w,h=480,340
canvas=Image.new('RGB',(w*3+80,h*6+150),'#f3f5f9')
d=ImageDraw.Draw(canvas)
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',16)
title=ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf',32)
d.text((25,20),'BuildWise | Design 02 Blueprint',font=title,fill='#14233b')
d.text((25,66),'Provider workspace · Midnight blue and orange · November 2026',font=font,fill='#778397')
for i,name in enumerate(names):
    im=Image.open(root/'screens'/f'{name}.png').convert('RGB')
    im=im.crop((0,0,im.width,min(im.height,1080)))
    im=ImageOps.contain(im,(w-15,h-45))
    x=25+(i%3)*(w+10);y=110+(i//3)*h
    canvas.paste(im,(x,y))
    d.text((x,y+h-37),f'{i+1:02d}  '+name.replace('-',' ').title(),font=font,fill='#14233b')
canvas.save(root/'BuildWise-screen-overview.png')

