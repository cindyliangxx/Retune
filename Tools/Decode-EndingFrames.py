import bpy
s=bpy.context.scene;s.sequence_editor_create();m=s.sequence_editor.sequences.new_movie('Reference',r'C:/Retune/Retune/Assets/ReTuneScenes/Art/Ending/TimingReference.mp4',1,1)
s.render.resolution_x=1920;s.render.resolution_y=1080;s.render.resolution_percentage=100;s.render.image_settings.file_format='PNG';s.view_settings.view_transform='Standard'
for f in range(1,m.frame_duration+1):
 s.frame_set(f);s.render.filepath='C:/Retune/Retune/Temp/EndingDecoded/Frame%02d.png'%f;bpy.ops.render.render(write_still=True)
