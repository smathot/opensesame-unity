import socket
import pygame
import json
import base64
import io
import time
from openexp._canvas.legacy import Legacy


def unity_command(command, message, data=None, address=('localhost', 8052)):
    """Connects to a Unity listener and sends a single command."""
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
        sock.connect(address)
        sock.sendall(json.dumps(
            {'command': command, 'message': str(message), 'data': data}
        ).encode('utf-8'))


def unity_prepare(fnc):
    """A decorator for Canvas.prepare() that sends the canvas as an image to
    the unity listener.
    """
    def inner(self):
        fnc(self)
        byte_stream = io.BytesIO()
        pygame.image.save(self.surface, byte_stream, "PNG")
        byte_stream.seek(0)
        encoded_string = base64.b64encode(byte_stream.read()).decode('utf-8')
        unity_command('image', id(self), encoded_string)
        time.sleep(.1)
    
    return inner


def unity_show(fnc):
    """A decorator for Canvas.show() that flips the canvas, which should have
    been prepared, to the Unity skybox.
    """
    def inner(self):
        unity_command('flip_skybox', id(self))
        return fnc(self)
    
    return inner


if canvas_backend != 'legacy':
    raise ValueError('Unity script required legacy backend')
Legacy.prepare = unity_prepare(Legacy.prepare)
Legacy.show = unity_show(Legacy.show)
unity_command('init_log', f'subject-{subject_nr}.log')
unity_command('skybox_size', 2000)
unity_command('skybox_color', 'background')
unity_command('log', 'start_phase 1')
