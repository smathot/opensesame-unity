import socket
import pygame
import json
import base64
import io
from openexp._canvas.legacy import Legacy


def send_to_unity(data, address=('localhost', 8052)):
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
        sock.connect(address)
        sock.sendall(json.dumps(data).encode('utf-8'))

        
def unity_log(msg):
    send_to_unity({'log': msg})


def unity_prepare(fnc):
    
    def inner(self):
        fnc(self)
        # Save the surface to a byte stream as PNG
        byte_stream = io.BytesIO()
        pygame.image.save(self.surface, byte_stream, "PNG")
        byte_stream.seek(0)  # Go back to the start of the byte stream
        # Encode the byte stream to base64
        encoded_string = base64.b64encode(byte_stream.read()).decode('utf-8')
        # Prepare JSON with image data
        image_data_json = {
            "image_data": {
                "id": id(self),
                "data": encoded_string
            }
        }
        send_to_unity(image_data_json)
        import time
        time.sleep(.1)
    
    return inner


def unity_show(fnc):
    
    def inner(self):
        image_data_json = {"flip_skybox": id(self)}
        send_to_unity(image_data_json)
        return fnc(self)
    
    return inner


Legacy.prepare = unity_prepare(Legacy.prepare)
Legacy.show = unity_show(Legacy.show)
unity_log('start_phase 1')
